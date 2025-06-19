using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
using Utilities.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UserService> _logger;
        private readonly IMemoryCache _cache;

        public UserService(IUserRepository userRepository, ILogger<UserService> logger, IMemoryCache memoryCache)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        }

        public async Task StoreOtpAsync(int userId, string otp)
        {
            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            };
            _cache.Set($"Otp_{userId}", otp, cacheEntryOptions);
            await Task.CompletedTask;
        }
        public async Task<UserProfileDto> GetUserProfileAsync(int userId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Retrieving profile for user {UserId}", userId);
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("User with ID {UserId} not found", userId);
                throw new Exception("User not found");
            }
            _logger.LogInformation("Successfully retrieved profile for user {UserId}", userId);

            return new UserProfileDto(
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email,
                user.Role
            );
        }

        public async Task<bool> UpdateUserProfileAsync(int userId, UpdateProfileDto dto, CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null) return false;

            user.FirstName = dto.FirstName ?? user.FirstName;
            user.LastName = dto.LastName ?? user.LastName;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user, cancellationToken);
            return true;
        }

        public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto, CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null) return false;

            if (!PasswordHelper.VerifyPassword(dto.OldPassword, user.PasswordHash))
                return false;

            user.PasswordHash = PasswordHelper.HashPassword(dto.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user, cancellationToken);
            return true;
        }

        public async Task<bool> DeleteUserAsync(int userId, CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null) return false;

            await _userRepository.DeleteAsync(userId, cancellationToken);
            return true;
        }

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync(CancellationToken cancellationToken = default)
        {
            var users = await _userRepository.GetAllUsersAsync(cancellationToken);
            return users.Select(u => new UserDto(u.Id, u.Email, u.Role));
        }

        public async Task<bool> SetUserRoleAsync(int userId, string role, CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null) return false;

            user.Role = role;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user, cancellationToken);
            return true;
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                _logger.LogWarning("Attempted to reset password for non-existent email: {Email}", request.Email);
                return false;
            }

            // OTP is stored with UserId as the key
            var otpKey = $"Otp_{user.Id}";
            if (!_cache.TryGetValue(otpKey, out string? cachedOtp) || cachedOtp != request.Otp)
            {
                _logger.LogWarning("Invalid or expired OTP for user {UserId}", user.Id);
                return false;
            }

            // OTP is valid, proceed with password reset
            user.PasswordHash = PasswordHelper.HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            _cache.Remove(otpKey); // Clean up the used OTP

            _logger.LogInformation("Password successfully reset for user {UserId}", user.Id);
            return true;
        }
    }
}