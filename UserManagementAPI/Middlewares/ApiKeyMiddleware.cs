using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using System.Collections.Generic;

namespace UserManagementAPI.Middlewares
{
    public class ApiKeyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        private const string SERVICE_KEY_HEADER_NAME = "X-Service-Key";

        public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _configuration = configuration;
        }        public async Task InvokeAsync(HttpContext context)
        {
            // Check if this is an internal service endpoint (like token validation or notifications)
            bool isInternalServiceEndpoint = context.Request.Path.StartsWithSegments("/api/v1/ums/auth/validate") || 
                                             context.Request.Path.StartsWithSegments("/api/v1/ums/notifications") ||
                                             context.Request.Path.StartsWithSegments("/api/auth/validate") ||
                                             context.Request.Path.StartsWithSegments("/api/notifications");
            
            if (isInternalServiceEndpoint)
            {
                // For inter-service communication, check for X-Service-Key
                if (!context.Request.Headers.TryGetValue(SERVICE_KEY_HEADER_NAME, out var serviceKey))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new { message = "Service API Key is missing" });
                    return;
                }

                // Get the internal API key - this should be the same value used in the payment service
                var internalApiKey = _configuration["InternalApiKey"] ?? "payment-service-key";
                if (serviceKey != internalApiKey)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsJsonAsync(new { message = "Invalid Service API Key" });
                    return;
                }
                
                // Add a flag to the HttpContext.Items to indicate the request has been validated
                // by ApiKeyMiddleware to prevent duplicate logging in JwtTokenValidationMiddleware
                context.Items["ApiKeyValidated"] = true;
                
                // Log here to have a single point of logging for service key validation
                if (context.RequestServices.GetService(typeof(ILogger<ApiKeyMiddleware>)) is ILogger<ApiKeyMiddleware> logger)
                {
                    logger.LogInformation("Validated service request with X-Service-Key: {Path}", context.Request.Path);
                }
            }
           
            // If the key is valid, continue processing the request
            await _next(context);
        }
    }

    // Extension method to register the middleware easily
    public static class ApiKeyMiddlewareExtensions
    {
        public static IApplicationBuilder UseApiKey(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ApiKeyMiddleware>();
        }
    }
}
