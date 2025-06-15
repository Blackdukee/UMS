using System;
using System.Threading.Tasks;
using Application.Interfaces;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public class GoogleAuthService : IGoogleAuthService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<GoogleAuthService> _logger;

        public GoogleAuthService(IConfiguration configuration, ILogger<GoogleAuthService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<(bool success, string? name, string? email)> VerifyGoogleTokenAsync(string idToken)
        {
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _configuration["Authentication:Google:ClientId"] }
                };

                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
                
                return (true, payload.Name, payload.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating Google token");
                return (false, null, null);
            }
        }
    }
}