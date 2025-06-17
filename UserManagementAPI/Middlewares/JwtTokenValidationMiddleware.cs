


using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace UserManagementAPI.Middlewares
{
    public class JwtTokenValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<JwtTokenValidationMiddleware> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _internalServiceKey; // Replace with your actual internal service key
        public JwtTokenValidationMiddleware(RequestDelegate next, ILogger<JwtTokenValidationMiddleware> logger, IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _configuration = configuration;
            _internalServiceKey = _configuration["X-Service-Key"] ?? "ETiPhW0E60m2jl5nyFK0iA";
        }
        public async Task InvokeAsync(HttpContext context)
        {

            if (context.Request.Path.StartsWithSegments("/swagger") ||
            context.Request.Path.StartsWithSegments("/api/v1/ums/auth/login") ||
            context.Request.Path.StartsWithSegments("/api/v1/ums/auth/google-login"))
            {
                // Skip JWT validation for these paths
                _logger.LogInformation("Skipping JWT validation for path: {Path}", context.Request.Path);
                await _next(context);
                return;
            }
            // Define all paths that should bypass JWT validation
            bool isAuthPath = context.Request.Path.StartsWithSegments("/api/v1/ums/auth") ||
                             context.Request.Path.StartsWithSegments("/api/auth");

            bool isNotificationsPath = context.Request.Path.StartsWithSegments("/api/v1/ums/notifications") ||
                                      context.Request.Path.StartsWithSegments("/api/notifications");

            bool hasValidServiceKey = context.Request.Headers.TryGetValue("X-Service-Key", out var serviceKey) &&
                                    serviceKey == _internalServiceKey;

            // Check if the ApiKeyMiddleware has already validated this request (for internal endpoints)
            bool isInternalServiceEndpoint = context.Request.Path.StartsWithSegments("/api/v1/ums/auth/validate") ||
                                           context.Request.Path.StartsWithSegments("/api/v1/ums/notifications") ||
                                           context.Request.Path.StartsWithSegments("/api/auth/validate") ||
                                           context.Request.Path.StartsWithSegments("/api/notifications");            // Check if ApiKeyMiddleware has already validated this request
            bool apiKeyValidated = context.Items.TryGetValue("ApiKeyValidated", out var validatedValue) && validatedValue is bool validated && validated;

            // Skip JWT validation for auth paths, notifications paths, or when a valid service key is provided
            if (isAuthPath || isNotificationsPath || hasValidServiceKey)
            {
                // Only log if the request hasn't been validated by ApiKeyMiddleware already
                if (!apiKeyValidated)
                {
                    if (hasValidServiceKey)
                    {
                        _logger.LogInformation("Skipping JWT validation for service with valid X-Service-Key: {Path}", context.Request.Path);
                    }
                    else if (isAuthPath)
                    {
                        _logger.LogInformation("Skipping JWT validation for Auth endpoint: {Path}", context.Request.Path);
                    }
                    else
                    {
                        _logger.LogInformation("Skipping JWT validation for Notifications endpoint: {Path}", context.Request.Path);
                    }
                }

                await _next(context);
                return;
            }


            //  (JWT)
            if (context.User?.Identity == null || !context.User.Identity.IsAuthenticated)
            {
                _logger.LogWarning("Unauthorized request - no valid JWT token provided for path: {Path}", context.Request.Path);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("{\"message\": \"No valid token provided\"}");
                return;
            }

            _logger.LogInformation("Authorized request for path: {Path}", context.Request.Path);


            await _next(context);
        }
    }

    public static class JwtTokenValidationMiddlewareExtensions
    {
        public static IApplicationBuilder UseJwtTokenValidation(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<JwtTokenValidationMiddleware>();
        }
    }
}
