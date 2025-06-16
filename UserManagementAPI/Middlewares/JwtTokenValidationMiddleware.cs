/*using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Logging;

namespace UserManagementAPI.Middlewares
{
    public class JwtTokenValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<JwtTokenValidationMiddleware> _logger;

        public JwtTokenValidationMiddleware(RequestDelegate next, ILogger<JwtTokenValidationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Skip validation for /api/Auth endpoints
            if (context.Request.Path.StartsWithSegments("/api/Auth"))
            {
                await _next(context);
                return;
            }

            // Check if token is already validated by JwtBearer
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                _logger.LogWarning("No valid token provided or authentication failed.");
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("{\"message\": \"No valid token provided\"}");
                return;
            }

            // Add extra validation if needed (e.g., custom claims check)
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            if (!string.IsNullOrEmpty(token))
            {
                try
                {
                    // Get the secret key from environment variable
                    var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? "X7kP9mQ2vL5jR8yT3wZ6nB4xC1uF8hJ9kLmP3qW4rT6yU8iO9pX2vC5mN7bV1j";
                    var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "http://localhost:5003";
                    var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "http://localhost:5003";

                    if (string.IsNullOrWhiteSpace(secretKey) || secretKey.Length < 32)
                    {
                        throw new InvalidOperationException("JWT SecretKey is not set or is too short in environment variables.");
                    }

                    var tokenHandler = new JwtSecurityTokenHandler();
                    var key = Encoding.UTF8.GetBytes(secretKey);
                    tokenHandler.ValidateToken(token, new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = true,
                        ValidIssuer = issuer,
                        ValidateAudience = true,
                        ValidAudience = audience,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    }, out _);

                    _logger.LogInformation("Token validated successfully for request: {Path}", context.Request.Path);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Token validation failed for request: {Path}", context.Request.Path);
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsync("{\"message\": \"Invalid or expired token\"}");
                    return;
                }
            }

            // Proceed to the next middleware or controller
            await _next(context);
        }
    }

    // Extension method to register the middleware
    public static class JwtTokenValidationMiddlewareExtensions
    {
        public static IApplicationBuilder UseJwtTokenValidation(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<JwtTokenValidationMiddleware>();
        }
    }
}*/





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

            if (context.Request.Path.StartsWithSegments("/api/v1/swagger"))
            {
                // Skip JWT validation for these paths
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
