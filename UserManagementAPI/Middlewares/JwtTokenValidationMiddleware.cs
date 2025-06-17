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
            // It's recommended to throw an exception if the key is missing, rather than using a hardcoded fallback.
            _internalServiceKey = _configuration["X-Service-Key"] ?? throw new ArgumentNullException("X-Service-Key is not configured.");
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Define a list of paths to exclude from JWT validation.
            var excludedPaths = new[]
            {
                "/swagger",
                "/api/v1/ums/auth",
                "/api/v1/ums/notifications"
            };

            // Skip validation for excluded paths.
            if (excludedPaths.Any(p => context.Request.Path.StartsWithSegments(p)))
            {
                _logger.LogInformation("Skipping JWT validation for excluded path: {Path}", context.Request.Path);
                await _next(context);
                return;
            }

            // Also, skip validation if a valid internal service key is present.
            if (context.Request.Headers.TryGetValue("X-Service-Key", out var serviceKey) && serviceKey == _internalServiceKey)
            {
                _logger.LogInformation("Skipping JWT validation for service request with valid X-Service-Key: {Path}", context.Request.Path);
                await _next(context);
                return;
            }

            // For all other paths, enforce JWT validation.
            if (context.User?.Identity == null || !context.User.Identity.IsAuthenticated)
            {
                _logger.LogWarning("Unauthorized: No valid JWT token for path: {Path}", context.Request.Path);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("{\"message\": \"A valid JWT token is required.\"}");
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
