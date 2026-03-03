using System.Security.Claims;
using System.Text;
using BLL.Services;
using DAL.Data;
using DAL.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using FinmateController.Hubs;

namespace FinmateController
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Controllers + JSON camelCase
            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            });

            builder.Services.AddSignalR();

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                // Thông tin cơ bản cho Swagger
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Finmate API",
                    Version = "v1",
                    Description = "Finmate Backend API (Clerk + Basic JWT)"
                });

                // Cấu hình nút Authorize với Bearer JWT
                var securityScheme = new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Nhập token dạng: Bearer {token}. Token có thể lấy từ /api/auth/login (Basic JWT) hoặc từ Clerk."
                };

                c.AddSecurityDefinition("Bearer", securityScheme);

                // Áp dụng security cho tất cả endpoint
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        securityScheme,
                        new[] { "Bearer" }
                    }
                });
            });

            // HttpClient
            builder.Services.AddHttpClient();

            // Register ClerkService với HttpClient riêng (GIỮ NGUYÊN CLERK)
            builder.Services.AddHttpClient<ClerkService>();

            // Gemini AI Chat - timeout 90s
            builder.Services.AddHttpClient<ChatService>()
                .ConfigureHttpClient(client =>
                {
                    client.Timeout = TimeSpan.FromSeconds(90);
                });

            // =======================
            // Auth: Clerk (giữ nguyên) + Basic (username/password)
            // =======================
            // Clerk JWT (OIDC metadata)
            var clerkInstanceUrl = builder.Configuration["Clerk:InstanceUrl"]
                                   ?? throw new InvalidOperationException("Clerk:InstanceUrl is not configured");
            var metadataAddress = $"{clerkInstanceUrl}/.well-known/openid-configuration";

            // Basic JWT (HS256) - chỉ dùng cho /api/auth/login trả token basic
            var basicJwtSecret = builder.Configuration["Jwt:SecretKey"]; // có thể cấu hình bằng AppSettings / env var
            var basicJwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "FinmateAPI";
            var basicJwtAudience = builder.Configuration["Jwt:Audience"] ?? "FinmateClient";

            builder.Services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Clerk";
                    options.DefaultChallengeScheme = "Clerk";
                })
                // Default giữ Clerk để không ảnh hưởng các endpoint cũ
                .AddJwtBearer("Clerk", options =>
                {
                    options.MetadataAddress = metadataAddress;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = clerkInstanceUrl,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        NameClaimType = "sub"
                    };
                })
                // Scheme Basic chỉ bật nếu có SecretKey (tránh crash khi deploy)
                .AddJwtBearer("Basic", options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = basicJwtIssuer,
                        ValidateAudience = true,
                        ValidAudience = basicJwtAudience,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = string.IsNullOrWhiteSpace(basicJwtSecret)
                            ? new SymmetricSecurityKey(Encoding.UTF8.GetBytes("00000000000000000000000000000000"))
                            : new SymmetricSecurityKey(Encoding.UTF8.GetBytes(basicJwtSecret)),
                        NameClaimType = ClaimTypes.NameIdentifier
                    };
                });

            builder.Services.AddAuthorization(options =>
            {
                // Policies (role claim sẽ được set bởi basic token)
                options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
                options.AddPolicy("StaffOrAbove", policy => policy.RequireRole("Admin", "Staff"));
                options.AddPolicy("UserOrAbove", policy => policy.RequireRole("Admin", "Staff", "User"));
            });

            // =======================
            // Database
            // =======================
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("DefaultConnection connection string is not configured");
            }

            builder.Services.AddDbContext<FinmateContext>(options =>
                options.UseNpgsql(connectionString));

            // Đăng ký Repository
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            // Các repository khác nếu project có (giữ nguyên phần bạn vừa pull)
            // (Nếu interface/class không tồn tại trong repo hiện tại, hãy comment lại để build pass.)
            builder.Services.AddScoped<IAccountTypeRepository, AccountTypeRepository>();
            builder.Services.AddScoped<ITransactionTypeRepository, TransactionTypeRepository>();
            builder.Services.AddScoped<IMoneySourceRepository, MoneySourceRepository>();
            builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
            builder.Services.AddScoped<IContactRepository, ContactRepository>();
            builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
            builder.Services.AddScoped<ICurrencyRepository, CurrencyRepository>();
            builder.Services.AddScoped<IGoalRepository, GoalRepository>();

            // Services
            builder.Services.AddScoped<UserService>();
            builder.Services.AddScoped<AuthService>(); // basic register/login service
            builder.Services.AddScoped<AccountTypeService>();
            builder.Services.AddScoped<CurrencyService>();
            builder.Services.AddScoped<MoneySourceService>();
            builder.Services.AddScoped<TransactionService>();
            builder.Services.AddScoped<TransactionTypeService>();
            builder.Services.AddScoped<CategoryService>();
            builder.Services.AddScoped<ReportService>();
            builder.Services.AddScoped<GoalService>();

            // CORS mở cho toàn bộ origin/port (tùy chỉnh sau qua cấu hình nếu cần khóa lại)
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            var app = builder.Build();

            // Cảnh báo nếu Mega LLM chưa cấu hình
            if (string.IsNullOrWhiteSpace(builder.Configuration["MegaLLM:ApiKey"]))
            {
                var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
                startupLogger.LogWarning("MegaLLM:ApiKey chưa được cấu hình. Thêm MegaLLM__ApiKey vào Azure Application Settings.");
            }

            // Swagger (bật mọi env)
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "FinmateController API v1");
                c.RoutePrefix = "swagger";
            });

            // 🔥 FIX PUBLISH AZURE: redirect root & index.html → Swagger
            app.MapGet("/", () => Results.Redirect("/swagger"))
               .ExcludeFromDescription();

            app.MapGet("/index.html", () => Results.Redirect("/swagger"))
               .ExcludeFromDescription();

            // Auto apply migrations
            ApplyPendingMigrations(app);

            // Seed admin user (tùy bạn có muốn bật lại không)
            // SeedAdminUser(app);

            app.UseHttpsRedirection();

            // Áp dụng CORS allow all
            app.UseCors("AllowAll");

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.MapHub<TransactionHub>("/hubs/transactions");

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
