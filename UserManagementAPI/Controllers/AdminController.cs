using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Application.Interfaces;
using Application.DTOs;

namespace UserManagementAPI.Controllers
{
    [Route("api")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IAdminService _adminService;
        
        public AdminController(IUserService userService, IAdminService adminService)
        {
            _userService = userService;
            _adminService = adminService;
        }        // GET /api/users - List all users
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userService.GetAllUsersAsync(CancellationToken.None);
            return Ok(users);
        }

        // GET /api/users/:id - Retrieve user details by ID
        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            try
            {
                var user = await _userService.GetUserProfileAsync(id, CancellationToken.None);
                return Ok(user);
            }
            catch (Exception ex)
            {
                return NotFound(new { message = "User not found", error = ex.Message });
            }
        }

        // PUT /api/users/:id - Update an existing user
        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateProfileDto dto)
        {
            var result = await _userService.UpdateUserProfileAsync(id, dto, CancellationToken.None);
            if (!result)
                return BadRequest(new { message = "Failed to update user." });
            return Ok(new { message = "User updated successfully." });
        }

        // DELETE /api/users/:id - Remove a user
        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var result = await _userService.DeleteUserAsync(id, CancellationToken.None);
            if (!result)
                return BadRequest(new { message = "Failed to delete user." });
            return Ok(new { message = "User deleted successfully." });
        }

        // POST /api/users/:id/suspend - Suspend a user account
        [HttpPost("users/{id}/suspend")]
        public async Task<IActionResult> SuspendUser(int id)
        {
            var result = await _adminService.SuspendUserAsync(id);
            if (!result)
                return BadRequest(new { message = "Failed to suspend user." });
            return Ok(new { message = "User suspended successfully." });
        }

        // POST /api/users/:id/activate - Activate a user account
        [HttpPost("users/{id}/activate")]
        public async Task<IActionResult> ActivateUser(int id)
        {
            var result = await _adminService.UnlockUserAsync(id);
            if (!result)
                return BadRequest(new { message = "Failed to activate user." });
            return Ok(new { message = "User activated successfully." });
        }

        // POST /api/users/activate-all - Activate all existing users (utility endpoint)
        [HttpPost("users/activate-all")]
        public async Task<IActionResult> ActivateAllUsers()
        {
            var result = await _adminService.ActivateAllExistingUsersAsync();
            if (!result)
                return BadRequest(new { message = "Failed to activate all users." });
            return Ok(new { message = "All users activated successfully." });
        }

        // Legacy endpoints for backward compatibility
        [HttpPut("v1/ums/admin/set-role/{userId}")]
        public async Task<IActionResult> SetUserRole(int userId, [FromBody] SetUserRoleDto dto)
        {
            var result = await _userService.SetUserRoleAsync(userId, dto.Role);
            if (!result)
                return BadRequest("Failed to update user role.");
            return Ok(new { message = "User role updated successfully." });
        }

        [HttpDelete("v1/ums/admin/delete-user/{userId}")]
        public async Task<IActionResult> DeleteUserLegacy(int userId)
        {
            var result = await _userService.DeleteUserAsync(userId);
            if (!result)
                return BadRequest("Failed to delete user.");
            return Ok(new { message = "User deleted successfully." });
        }
    }
}
