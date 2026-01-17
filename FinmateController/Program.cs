
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
                    options.MetadataAddress = metadataAddress;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = clerkInstanceUrl,
                        ValidateAudience = false, // Clerk tokens không có audience
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        NameClaimType = "sub"
                    };
                });

            builder.Services.AddAuthorization();

            // Cấu hình DbContext
            builder.Services.AddDbContext<FinmateContext>(options =>
                options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

            // Đăng ký Repository
            builder.Services.AddScoped<IUserRepository, UserRepository>();

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

            // Auto apply EF Core migrations at startup
            ApplyPendingMigrations(app);

            app.UseHttpsRedirection();

            // Exception handling middleware - phải đặt trước UseAuthentication/UseAuthorization
            app.UseExceptionHandler(errorApp =>
            {
                errorApp.Run(async context =>
                {
                    context.Response.StatusCode = 500;
                    context.Response.ContentType = "application/json";

                    var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
                    var exception = exceptionHandlerPathFeature?.Error;

                    if (exception != null)
                    {
                        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                        logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

                        var errorResponse = new
                        {
                            error = "Internal server error",
                            message = exception.Message,
                            innerError = exception.InnerException?.Message,
                            stackTrace = exception.StackTrace,
                            timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC")
                        };

                        await context.Response.WriteAsJsonAsync(errorResponse);
                    }
                });
            });

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }

        //Automatic migration
        private static void ApplyPendingMigrations(WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<FinmateContext>();
            dbContext.Database.Migrate();
        }
    }
}
