

using Microsoft.IdentityModel.Tokens;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Utilities.Security
{
    public static class JwtHelper
    {
        private static readonly string SecretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? "X7kP9mQ2vL5jR8yT3wZ6nB4xC1uF8hJ9kLmP3qW4rT6yU8iO9pX2vC5mN7bV1j";

        public static (bool isValid, ClaimsPrincipal? claimsPrincipal) ValidateToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(SecretKey);
            
            try
            {
                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "http://localhost:5003",
                    ValidateAudience = true,
                    ValidAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "http://localhost:5003",
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                var claimsPrincipal = tokenHandler.ValidateToken(token, validationParameters, out _);
                return (true, claimsPrincipal);
            }
            catch
            {
                return (false, null);
            }
        }

        public static string GenerateToken(int userId, string email, string role, TimeSpan expiration)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(SecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role)
            };

            var token = new JwtSecurityToken(
                issuer: Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "http://localhost:5003",
                audience: Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "http://localhost:5003",
                claims: claims,
                expires: DateTime.UtcNow.Add(expiration),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}