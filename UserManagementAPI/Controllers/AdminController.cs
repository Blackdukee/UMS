using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Threading.Tasks;
using Application.Interfaces;
using Application.DTOs;

namespace UserManagementAPI.Controllers
{
    [Route("api/v1/ums/admin")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly IUserService _userService;
        public AdminController(IUserService userService)
        {
            _userService = userService;
        }
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _userService.GetAllUsersAsync(CancellationToken.None);
            return Ok(users);
        }
        [HttpPut("set-role/{userId}")]
        public async Task<IActionResult> SetUserRole(int userId, [FromBody] SetUserRoleDto dto)
        {
            var result = await _userService.SetUserRoleAsync(userId, dto.Role);
            if (!result)
                return BadRequest("Failed to update user role.");
            return Ok(new { message = "User role updated successfully." });
        }
        [HttpDelete("delete-user/{userId}")]
        public async Task<IActionResult> DeleteUser(int userId)
        {
            var result = await _userService.DeleteUserAsync(userId);
            if (!result)
                return BadRequest("Failed to delete user.");
            return Ok(new { message = "User deleted successfully." });
        }
    }
}
