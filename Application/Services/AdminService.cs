using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Application.Services
{
    public class AdminService : IAdminService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<AdminService> _logger;

        public AdminService(IUserRepository userRepository, ILogger<AdminService> logger)
        {
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<UserDto?> GetUserByIdAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return null;
            return new UserDto(user.Id, user.Email, user.Role);
        }

        public async Task<bool> UpdateUserAsync(int userId, UpdateProfileDto dto)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return false;

            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            return true;
        }

        public async Task<IEnumerable<UserDto>> SearchUsersAsync(UserFilterDto filter)
        {
            var users = await _userRepository.GetAllUsersAsync();

            if (filter != null)
            {
                // page and limit 
                
                if (filter.Page > 0 && filter.Limit > 0)
                {
                    users = users.Skip((filter.Page - 1) * filter.Limit).Take(filter.Limit);
                }

                if (!string.IsNullOrEmpty(filter.Role))
                {
                    users = users.Where(u => u.Role.Equals(filter.Role, StringComparison.OrdinalIgnoreCase));
                }

                if (filter.IsActive.HasValue)
                {
                    users = users.Where(u => u.IsActive == filter.IsActive.Value);
                }
            }

            return users.Select(u => new UserDto(u.Id, u.Email, u.Role));
        }

        public async Task<bool> ActivateUserAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return false;

            user.IsActive = true;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepository.UpdateAsync(user);
            return true;
        }

        public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
        {
            try
            {
                _logger.LogInformation("Retrieving all users");
                var users = await _userRepository.GetAllUsersAsync();

                var userDtos = users.Select(u => new UserDto(u.Id, u.Email, u.Role)).ToList();

                _logger.LogInformation("Successfully retrieved {Count} users", userDtos.Count);
                return userDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all users");
                throw;
            }
        }

        public async Task<bool> SetUserRoleAsync(int userId, string role)
        {
            try
            {
                _logger.LogInformation("Setting role {Role} for user {UserId}", role, userId);

                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found", userId);
                    return false;
                }

                user.Role = role;
                user.UpdatedAt = DateTime.UtcNow;

                await _userRepository.UpdateAsync(user);

                _logger.LogInformation("Successfully updated role for user {UserId} to {Role}", userId, role);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting role for user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            try
            {
                _logger.LogInformation("Deleting user {UserId}", userId);

                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found", userId);
                    return false;
                }

                await _userRepository.DeleteAsync(userId);

                _logger.LogInformation("Successfully deleted user {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> ResetPasswordAsync(int userId, string newPassword)
        {
            try
            {
                _logger.LogInformation("Resetting password for user {UserId}", userId);

                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found for password reset", userId);
                    return false;
                }

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                user.UpdatedAt = DateTime.UtcNow;

                await _userRepository.UpdateAsync(user);

                _logger.LogInformation("Successfully reset password for user {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> SuspendUserAsync(int userId)
        {
            try
            {
                _logger.LogInformation("Suspending user {UserId}", userId);

                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found for suspension", userId);
                    return false;
                }

                user.IsActive = false; // Deactivate the user
                user.UpdatedAt = DateTime.UtcNow;

                await _userRepository.UpdateAsync(user);

                _logger.LogInformation("Successfully suspended user {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error suspending user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> UnlockUserAsync(int userId)
        {
            try
            {
                _logger.LogInformation("Unlocking user {UserId}", userId);

                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found for unlocking", userId);
                    return false;
                }

                user.IsActive = true; // Reactivate the user
                user.UpdatedAt = DateTime.UtcNow;

                await _userRepository.UpdateAsync(user);

                _logger.LogInformation("Successfully unlocked user {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unlocking user {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> ActivateAllExistingUsersAsync()
        {
            try
            {
                _logger.LogInformation("Activating all existing users.");
                var allUsers = await _userRepository.GetAllUsersAsync();
                foreach (var user in allUsers)
                {
                    user.IsActive = true;
                }
                await _userRepository.UpdateRangeAsync(allUsers);
                _logger.LogInformation("Successfully activated all existing users.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while activating all existing users.");
                return false;
            }
        }
    }
}
