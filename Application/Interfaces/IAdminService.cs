using Application.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IAdminService
    {
        // Retrieves all users in the system.
        Task<IEnumerable<UserDto>> GetAllUsersAsync();

        // Sets a new role for a specified user.
        Task<bool> SetUserRoleAsync(int userId, string role);

        // Deletes a user account.
        Task<bool> DeleteUserAsync(int userId);

        // Resets the password for a user (admin initiated).
        Task<bool> ResetPasswordAsync(int userId, string newPassword);

        // Suspends a user account (to temporarily block access).
        Task<bool> SuspendUserAsync(int userId);

        // Unlocks or reactivates a suspended user account.
        Task<bool> UnlockUserAsync(int userId);

        // Activates all existing users (utility method to fix migration issues)
        Task<bool> ActivateAllExistingUsersAsync();

        // Gets a user by their ID.
        Task<UserDto?> GetUserByIdAsync(int userId);

        // Updates a user's information.
        Task<bool> UpdateUserAsync(int userId, UpdateProfileDto dto);

        // Searches for users based on provided filters.
        Task<IEnumerable<UserDto>> SearchUsersAsync(UserFilterDto filter);

        // Activates a user account.
        Task<bool> ActivateUserAsync(int userId);
    }
}
