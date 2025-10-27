using FairyTaleExplorer.DTOs;
using MainServer.Exceptions;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace MainServer.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var response = new ErrorResponse();

            switch (exception)
            {
                case NotFoundException:
                    response.Message = exception.Message;
                    response.StatusCode = StatusCodes.Status404NotFound;
                    break;
                case UnauthorizedException:
                    response.Message = "Unauthorized access";
                    response.StatusCode = StatusCodes.Status401Unauthorized;
                    break;
                case Exceptions.ValidationException:
                    response.Message = exception.Message;
                    response.StatusCode = StatusCodes.Status400BadRequest;
                    break;
                case ServiceException:
                    response.Message = "A service error occurred";
                    response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                    break;
                default:
                    response.Message = "An error occurred while processing your request";
                    response.StatusCode = StatusCodes.Status500InternalServerError;
                    break;
            }

            context.Response.StatusCode = response.StatusCode;

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
        }
    }
}