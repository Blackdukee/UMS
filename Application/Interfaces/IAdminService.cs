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

        // Searches for users based on a search query ( name or email).
        Task<IEnumerable<UserDto>> SearchUsersAsync(string query);

        // Suspends a user account (to temporarily block access).
        Task<bool> SuspendUserAsync(int userId);

        // Unlocks or reactivates a suspended user account.
        Task<bool> UnlockUserAsync(int userId);

        // Activates all existing users (utility method to fix migration issues)
        Task<bool> ActivateAllExistingUsersAsync();
    }
}
