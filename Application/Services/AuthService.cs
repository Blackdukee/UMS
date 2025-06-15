

using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Utilities.Security;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

namespace Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IGoogleAuthService _googleAuthService;

        public AuthService(IUserRepository userRepository, IGoogleAuthService googleAuthService)
        {
            _googleAuthService = googleAuthService;
            _userRepository = userRepository;
        }

        public async Task<string> RegisterAsync(RegisterDto dto)
        {
            var existingUser = await _userRepository.GetByEmailAsync(dto.Email);
            if (existingUser != null)
            {
                return "User already exists. Please log in or use a different email.";
            }

            var user = new User
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                PasswordHash = PasswordHelper.HashPassword(dto.Password),
                Role = dto.Role,
            };

            await _userRepository.AddAsync(user);
            return "User registered successfully.";
        }

        public async Task<LoginResponseDto> LoginAsync(LoginDto dto)
        {
            var user = await _userRepository.GetByEmailAsync(dto.Email);
            if (user == null || !PasswordHelper.VerifyPassword(dto.Password, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Invalid credentials. Please check your email or password.");
            }

            var accessToken = JwtHelper.GenerateToken(user.Id, user.Email, user.Role, TimeSpan.FromMinutes(15));
            var refreshToken = GenerateRefreshToken();
            await _userRepository.StoreRefreshTokenAsync(user.Id, refreshToken, DateTime.UtcNow.AddDays(7));

            return new LoginResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
        }

        public async Task<LoginResponseDto> LoginWithGoogleAsync(GoogleLoginDto googleLoginDto)
        {
            // Verify Google token
            var (success, name, email) = await _googleAuthService.VerifyGoogleTokenAsync(googleLoginDto.IdToken?? throw new ArgumentNullException(nameof(googleLoginDto.IdToken)));
            if (!success || string.IsNullOrEmpty(email))
            {
                throw new UnauthorizedAccessException("Invalid Google token.");
            }

            // Check if user exists
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                // Register new user
                user = new User
                {
                    FirstName = name ?? email.Split('@')[0],
                    LastName = name ?? email.Split('@')[0],
                    Email = email,
                    Role = "Student", // Default role for new users
                    CreatedAt = DateTime.UtcNow
                };
                await _userRepository.AddAsync(user);
            }

            // Generate tokens
            var accessToken = JwtHelper.GenerateToken(user.Id, user.Email, user.Role, TimeSpan.FromMinutes(15));
            var refreshToken = GenerateRefreshToken();
            await _userRepository.StoreRefreshTokenAsync(user.Id, refreshToken, DateTime.UtcNow.AddDays(7));

            return new LoginResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
        }

        public async Task<LoginResponseDto> RefreshTokenAsync(string refreshToken)
        {
            // Get userId from repository
            var userId = await _userRepository.GetUserIdByRefreshTokenAsync(refreshToken);

            // Verify stored token
            var storedToken = await _userRepository.GetRefreshTokenAsync(userId);
            if (storedToken != refreshToken)
                throw new SecurityTokenException("Invalid refresh token");

            // Revoke all existing tokens for user
            await _userRepository.RevokeRefreshTokenAsync(userId);

            // Fetch user details
            var user = await _userRepository.GetByIdAsync(userId) ?? throw new SecurityTokenException("User not found");

            // Generate new tokens
            var newAccessToken = JwtHelper.GenerateToken(user.Id, user.Email, user.Role, TimeSpan.FromMinutes(15));
            var newRefreshToken = GenerateRefreshToken();
            await _userRepository.StoreRefreshTokenAsync(userId, newRefreshToken, DateTime.UtcNow.AddDays(7));

            return new LoginResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            };
        }

        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
}
