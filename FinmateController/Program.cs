using System.Security.Claims;
using System.Text;
using DAL.Data;
using DAL.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using BLL.Services;

namespace FinmateController
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // =======================
            // Add services
            // =======================

            builder.Services.AddControllers();

            // Swagger
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // HttpClient
            builder.Services.AddHttpClient();
            builder.Services.AddHttpClient<ClerkService>();

            // JWT Authentication (Basic - không dùng Clerk)
            var jwtSecret = builder.Configuration["Jwt:SecretKey"]
                ?? throw new InvalidOperationException("Jwt:SecretKey is not configured");
            
            // Validate JWT SecretKey length (ít nhất 32 ký tự cho security)
            if (jwtSecret.Length < 32)
            {
                throw new InvalidOperationException("Jwt:SecretKey must be at least 32 characters long for security");
            }
            
            var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "FinmateAPI";
            var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "FinmateClient";

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwtIssuer,
                        ValidateAudience = true,
                        ValidAudience = jwtAudience,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                        NameClaimType = ClaimTypes.NameIdentifier
                    };
                });

            builder.Services.AddAuthorization(options =>
            {
                // Policy cho Admin - quyền cao nhất
                options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));

                // Policy cho Staff trở lên (Staff và Admin)
                options.AddPolicy("StaffOrAbove", policy => policy.RequireRole("Admin", "Staff"));

                // Policy cho User trở lên (tất cả user đã đăng nhập)
                options.AddPolicy("UserOrAbove", policy => policy.RequireRole("Admin", "Staff", "User"));
            });

            // Database
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("DefaultConnection connection string is not configured");
            }
            
            builder.Services.AddDbContext<FinmateContext>(options =>
                options.UseNpgsql(connectionString));

            // Repositories
            builder.Services.AddScoped<IUserRepository, UserRepository>();

            // Services
            builder.Services.AddScoped<UserService>();
            builder.Services.AddScoped<AuthService>();

            var app = builder.Build();

            // =======================
            // Middleware pipeline
            // =======================

            // 🔥 FIX PUBLISH AZURE: redirect root & index.html → Swagger
            // Dùng middleware để redirect ngay từ đầu pipeline
            app.Use(async (context, next) =>
            {
                var path = context.Request.Path.Value?.ToLower();
                if (path == "/" || path == "/index.html")
                {
                    context.Response.Redirect("/swagger/index.html", permanent: false);
                    return;
                }
                await next();
            });

            // MapGet như backup (nếu middleware không catch được)
            app.MapGet("/", () => Results.Redirect("/swagger/index.html", permanent: false))
               .ExcludeFromDescription();

            app.MapGet("/index.html", () => Results.Redirect("/swagger/index.html", permanent: false))
               .ExcludeFromDescription();

            // 👉 LUÔN bật Swagger (kể cả Azure)
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "FinmateController API v1");
                c.RoutePrefix = "swagger";
            });

            // Auto apply migrations
            ApplyPendingMigrations(app);

            // Seed admin user
            SeedAdminUser(app);

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }

        // =======================
        // Auto migrate database
        // =======================
        private static void ApplyPendingMigrations(WebApplication app)
        {
            try
            {
                using var scope = app.Services.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<FinmateContext>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                
                // Kiểm tra database có sẵn sàng không
                if (!dbContext.Database.CanConnect())
                {
                    logger.LogWarning("Cannot connect to database. Skipping migrations.");
                    return;
                }
                
                logger.LogInformation("Applying database migrations...");
                dbContext.Database.Migrate();
                logger.LogInformation("Database migrations applied successfully");
            }
            catch (Exception ex)
            {
                // Log error nhưng không crash app - có thể migration đã được apply rồi
                var logger = app.Services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "Error applying migrations. This might be expected if migrations are already applied. Error: {Message}", ex.Message);
            }
        }

        // =======================
        // Seed admin user
        // =======================
        private static void SeedAdminUser(WebApplication app)
        {
            try
            {
                using var scope = app.Services.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<FinmateContext>();
                var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

                // Kiểm tra database có sẵn sàng không
                if (!dbContext.Database.CanConnect())
                {
                    logger.LogWarning("Cannot connect to database. Skipping admin user seed.");
                    return;
                }

                // Kiểm tra admin user đã tồn tại chưa
                // Dùng "admin@admin.com" làm email để hợp lệ với email validation
                var adminEmail = "admin@admin.com";
                var existingAdmin = userRepository.GetByEmailAsync(adminEmail).Result;

                if (existingAdmin == null)
                {
                    // Tạo admin user với Role.Admin
                    var adminUser = new DAL.Models.Users
                    {
                        Email = adminEmail,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                        FullName = "Administrator",
                        IsActive = true,
                        IsPremium = false,
                        Role = DAL.Models.Role.Admin,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    userRepository.AddAsync(adminUser).Wait();
                    logger.LogInformation("Admin user created successfully");
                }
                else
                {
                    var updated = false;
                    // Đảm bảo admin user có quyền Admin và password đúng
                    if (existingAdmin.Role != DAL.Models.Role.Admin)
                    {
                        existingAdmin.Role = DAL.Models.Role.Admin;
                        updated = true;
                    }

                    // Cập nhật password nếu cần (để đảm bảo password là 123456)
                    try
                    {
                        if (string.IsNullOrEmpty(existingAdmin.PasswordHash) || 
                            !BCrypt.Net.BCrypt.Verify("123456", existingAdmin.PasswordHash))
                        {
                            existingAdmin.PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456");
                            existingAdmin.UpdatedAt = DateTime.UtcNow;
                            updated = true;
                        }
                    }
                    catch (Exception bcryptEx)
                    {
                        // Nếu BCrypt verify fail (hash format không đúng), reset password
                        logger.LogWarning(bcryptEx, "BCrypt verify failed, resetting admin password");
                        existingAdmin.PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456");
                        existingAdmin.UpdatedAt = DateTime.UtcNow;
                        updated = true;
                    }

                    if (updated)
                    {
                        userRepository.UpdateAsync(existingAdmin).Wait();
                        logger.LogInformation("Admin user updated successfully");
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error nhưng không crash app
                var logger = app.Services.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "Error seeding admin user. This might be expected if admin already exists.");
            }
        }
    }
}
