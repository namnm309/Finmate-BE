using DAL.Data;
using DAL.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using BLL.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace FinmateController
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    // Cấu hình JSON serializer để dùng camelCase (để sync với mobile app)
                    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                    options.JsonSerializerOptions.WriteIndented = true;
                });
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // HttpClient
            builder.Services.AddHttpClient();

            // Register ClerkService với HttpClient riêng
            builder.Services.AddHttpClient<ClerkService>();

            // Cấu hình JWT Authentication với Clerk
            var clerkInstanceUrl = builder.Configuration["Clerk:InstanceUrl"] ?? throw new InvalidOperationException("Clerk:InstanceUrl is not configured");
            var metadataAddress = $"{clerkInstanceUrl}/.well-known/openid-configuration";

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = clerkInstanceUrl,
                        ValidateAudience = false,
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

            // Database
            builder.Services.AddDbContext<FinmateContext>(options =>
                options.UseNpgsql(connectionString));

            // Đăng ký Repository
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IAccountTypeRepository, AccountTypeRepository>();
            builder.Services.AddScoped<ITransactionTypeRepository, TransactionTypeRepository>();
            builder.Services.AddScoped<IMoneySourceRepository, MoneySourceRepository>();
            builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
            builder.Services.AddScoped<IContactRepository, ContactRepository>();
            builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
            builder.Services.AddScoped<ICurrencyRepository, CurrencyRepository>();

            // Đăng ký Services
            builder.Services.AddScoped<UserService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            // Enable Swagger in all environments (including production)
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "TechStore API v1");
                c.RoutePrefix = "swagger"; // Swagger UI will be available at /swagger
                c.DisplayRequestDuration();
            });

            // 👉 LUÔN bật Swagger (kể cả Azure)
            app.UseSwagger();
            app.UseSwaggerUI();

            // 🔥 FIX PUBLISH AZURE: redirect root & index.html → Swagger
            app.MapGet("/", () => Results.Redirect("/swagger"))
               .ExcludeFromDescription();

            app.MapGet("/index.html", () => Results.Redirect("/swagger"))
               .ExcludeFromDescription();

            // Auto apply migrations
            ApplyPendingMigrations(app);

            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }

        //Automatic migration
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
