using MainServer.BackgroundServices;
using MainServer.Service;
using MainServer.Service.AI;
using MainServer.Service.GoogleDrive;
using MainServer.Service.Recommend;
using MainServer.Service.Stroy;
using MainServer.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace FairyTaleExplorer.Configuration
{
    public static class ServiceExtensions
    {
        public static IServiceCollection AddCustomServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Database
            services.AddDbContext<MainServerDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            // Services
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IStoryService, StoryService>();
            services.AddScoped<IRecommendationService, RecommendationService>();
            services.AddScoped<IGoogleDriveService, GoogleDriveService>();
            services.AddScoped<IAIService, AIService>();

            // Background Services
            services.AddHostedService<StoryProcessingService>();
            services.AddHostedService<RecommendationUpdateService>();

            // SignalR
            services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
                options.MaximumReceiveMessageSize = 102400; // 100KB
            });

            // Caching
            services.AddMemoryCache();
            services.AddDistributedMemoryCache();

            // HttpClient
            services.AddHttpClient<IAIService, AIService>();

            return services;
        }

        public static IServiceCollection AddCustomAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = configuration["Jwt:Issuer"],
                        ValidAudience = configuration["Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(configuration["Jwt:Key"]))
                    };

                    // SignalR JWT 설정
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            var accessToken = context.Request.Query["access_token"];
                            var path = context.HttpContext.Request.Path;

                            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/storyHub"))
                            {
                                context.Token = accessToken;
                            }

                            return Task.CompletedTask;
                        }
                    };
                })
                .AddGoogle(options =>
                {
                    options.ClientId = configuration["Authentication:Google:ClientId"];
                    options.ClientSecret = configuration["Authentication:Google:ClientSecret"];
                });

            return services;
        }
    }
}