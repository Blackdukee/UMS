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
        private readonly IAdminService _adminService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(IAdminService adminService, ILogger<AdminController> logger)
        {
            _adminService = adminService;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET /api/users - List all users
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers([FromQuery] UserFilterDto filters)
        {
            var filter = filters ?? new UserFilterDto(); // Ensure filter is not null
            if (filter.Page <= 0) filter.Page = 10; // Default page size
            if (filter.Limit <= 0) filter.Limit = 1; // Default page number

            // Log the request for debugging purposes
            _logger.LogInformation("Received request to get all users with filters: {@Filters}", filter);
            
            var users = await _adminService.SearchUsersAsync(filter);
            return Ok(users);
        }
 
        // GET /api/users/:id - Retrieve user details by ID
        [HttpGet("users/{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            try
            {
                var user = await _adminService.GetUserByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by ID {UserId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        // PUT /api/users/:id - Update an existing user
        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateProfileDto dto)
        {
            var result = await _adminService.UpdateUserAsync(id, dto);
            if (!result)
                return BadRequest(new { message = "Failed to update user." });
            return Ok(new { message = "User updated successfully." });
        }

        // DELETE /api/users/:id - Remove a user
        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var result = await _adminService.DeleteUserAsync(id);
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
            var result = await _adminService.ActivateUserAsync(id);
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
            var result = await _adminService.SetUserRoleAsync(userId, dto.Role);
            if (!result)
                return BadRequest("Failed to update user role.");
            return Ok(new { message = "User role updated successfully." });
        }

        [HttpDelete("v1/ums/admin/delete-user/{userId}")]
        public async Task<IActionResult> DeleteUserLegacy(int userId)
        {
            var result = await _adminService.DeleteUserAsync(userId);
            if (!result)
                return BadRequest("Failed to delete user.");
            return Ok(new { message = "User deleted successfully." });
        }
    }
}
