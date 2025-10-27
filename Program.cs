using FairyTaleExplorer.Configuration;
using MainServer.Data;
using MainServer.Hubs;
using MainServer.Middleware;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // Custom services
        builder.Services.AddCustomServices(builder.Configuration);
        object value = builder.Services.AddCustomAuthentication(builder.Configuration);

        // CORS
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
            {
                policy.WithOrigins("http://localhost:3000", "https://yourdomain.com")
                       .AllowAnyMethod()
                       .AllowAnyHeader()
                       .AllowCredentials();
            });
        });

        var app = builder.Build();

        // Configure pipeline
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseCors("AllowAll");
        app.UseAuthentication();
        app.UseAuthorization();

        // Middleware
        app.UseMiddleware<ExceptionMiddleware>();

        // Endpoints
        app.MapControllers();
        app.MapHub<StoryHub>("/storyHub");

        // Database migration
        using (var scope = app.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<MainServerDbContext>();
            context.Database.Migrate();
        }

        app.Run();
    }
}