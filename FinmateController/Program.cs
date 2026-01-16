using DAL.Data;
using DAL.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
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

            // JWT Authentication (Clerk)
            var clerkInstanceUrl = builder.Configuration["Clerk:InstanceUrl"]
                ?? throw new InvalidOperationException("Clerk:InstanceUrl is not configured");

            var metadataAddress = $"{clerkInstanceUrl}/.well-known/openid-configuration";

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
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
                });

            builder.Services.AddAuthorization();

            // Database
            builder.Services.AddDbContext<FinmateContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Repositories
            builder.Services.AddScoped<IUserRepository, UserRepository>();

            // Services
            builder.Services.AddScoped<UserService>();

            var app = builder.Build();

            // =======================
            // Middleware pipeline
            // =======================

            // 👉 LUÔN bật Swagger (kể cả Azure)
            app.UseSwagger();
            app.UseSwaggerUI();

            // Auto apply migrations
            ApplyPendingMigrations(app);

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
            using var scope = app.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<FinmateContext>();
            dbContext.Database.Migrate();
        }
    }
}
