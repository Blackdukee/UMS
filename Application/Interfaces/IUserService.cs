using Application.DTOs;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IUserService
    {
        /// <summary>
        /// Retrieves the profile for a given user.
        /// </summary>
        /// <param name="userId">The user’s ID.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>An immutable UserProfileDto.</returns>
        Task<UserProfileDto> GetUserProfileAsync(int userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates the profile information for a user.
        /// </summary>
        /// <param name="userId">The user’s ID.</param>
        /// <param name="dto">Data for updating the profile.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>True if update succeeded; otherwise, false.</returns>
        Task<bool> UpdateUserProfileAsync(int userId, UpdateProfileDto dto, CancellationToken cancellationToken = default);

        /// <summary>
        /// Changes the password for a user after verifying the old password.
        /// </summary>
        /// <param name="userId">The user’s ID.</param>
        /// <param name="dto">Data for changing the password.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>True if password change succeeded; otherwise, false.</returns>
        Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a user account.
        /// </summary>
        /// <param name="userId">The user’s ID.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>True if deletion succeeded; otherwise, false.</returns>
        Task<bool> DeleteUserAsync(int userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves a list of all users.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>An enumerable of UserDto objects.</returns>
        Task<IEnumerable<UserDto>> GetAllUsersAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets a new role for a specified user.
        /// </summary>
        /// <param name="userId">The user’s ID.</param>
        /// <param name="role">The new role (e.g., "Admin", "Instructor", "Student").</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <returns>True if the role update succeeded; otherwise, false.</returns>
        Task<bool> SetUserRoleAsync(int userId, string role, CancellationToken cancellationToken = default);
        Task StoreOtpAsync(int userId, string otp);
        Task<bool> ResetPasswordAsync(ResetPasswordRequest request);
    }
}
