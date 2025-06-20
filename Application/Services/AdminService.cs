using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Utilities.Security;
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
                    _logger.LogWarning("User {UserId} not found", userId);
                    return false;
                }

                user.PasswordHash = PasswordHelper.HashPassword(newPassword);
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

        public async Task<IEnumerable<UserDto>> SearchUsersAsync(string keyword)
        {
            try
            {
                _logger.LogInformation("Searching users with keyword: {Keyword}", keyword);

                if (string.IsNullOrWhiteSpace(keyword))
                {
                    return await GetAllUsersAsync();
                }

                var users = await _userRepository.GetAllUsersAsync();

                var filteredUsers = users.Where(u =>
                    u.Email.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    u.FirstName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    u.LastName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                    u.Role.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                ).ToList();

                var userDtos = filteredUsers.Select(u => new UserDto(u.Id, u.Email, u.Role)).ToList();

                _logger.LogInformation("Found {Count} users matching keyword: {Keyword}", userDtos.Count, keyword);
                return userDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching users with keyword: {Keyword}", keyword);
                throw;
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
                    _logger.LogWarning("User {UserId} not found", userId);
                    return false;
                }                // Suspend the user account by setting IsActive to false
                user.IsActive = false;
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
                _logger.LogInformation("Unlocking/activating user {UserId}", userId);

                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found", userId);
                    return false;
                }

                // Activate the user account by setting IsActive to true
                user.IsActive = true;
                user.UpdatedAt = DateTime.UtcNow;

                await _userRepository.UpdateAsync(user);

                _logger.LogInformation("Successfully unlocked/activated user {UserId}", userId);
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
                _logger.LogInformation("Activating all existing users");

                var users = await _userRepository.GetAllUsersAsync();
                var inactiveUsers = users.Where(u => !u.IsActive).ToList();

                if (!inactiveUsers.Any())
                {
                    _logger.LogInformation("No inactive users found");
                    return true;
                }

                foreach (var user in inactiveUsers)
                {
                    user.IsActive = true;
                    user.UpdatedAt = DateTime.UtcNow;
                    await _userRepository.UpdateAsync(user);
                }

                _logger.LogInformation("Successfully activated {Count} users", inactiveUsers.Count);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating all existing users");
                return false;
            }
        }
    }

}
