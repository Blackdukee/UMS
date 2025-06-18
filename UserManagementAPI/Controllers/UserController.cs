using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces;
using Application.DTOs;
using Azure;
using Microsoft.Extensions.Logging;

namespace UserManagementAPI.Controllers
{
    [Route("api/v1/ums/user")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IEmailService _emailService;
        
        private readonly ILogger<UserController> _logger;

        public UserController(IUserService userService, IEmailService emailService, ILogger<UserController> logger)
        {
            _logger = logger;
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));
            _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        }

     
        private bool TryGetUserId(out int userId)
        {
            userId = 0;
            string? userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return !string.IsNullOrEmpty(userIdStr) && int.TryParse(userIdStr, out userId);
        }

        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
        {
            _logger.LogInformation("GetProfile called by user with ID: {UserId}", User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (!TryGetUserId(out int userId))
            {
                _logger.LogWarning("User ID is missing or invalid.");
                return Unauthorized("User ID is missing or invalid.");
            }

            var profile = await _userService.GetUserProfileAsync(userId, cancellationToken);
            return Ok(profile);
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            if (!TryGetUserId(out int userId))
            {
                return Unauthorized("User ID is missing or invalid.");
            }

            var result = await _userService.UpdateUserProfileAsync(userId, dto);
            if (!result)
                return BadRequest("Failed to update profile.");
            return Ok(new { message = "Profile updated successfully." });
        }

        [HttpPut("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            if (!TryGetUserId(out int userId))
            {
                return Unauthorized("User ID is missing or invalid.");
            }

            var result = await _userService.ChangePasswordAsync(userId, dto);
            if (!result)
                return BadRequest("Failed to change password. Check your old password.");
            return Ok(new { message = "Password changed successfully." });
        }

        [HttpDelete("delete-account")]
        public async Task<IActionResult> DeleteAccount()
        {
            if (!TryGetUserId(out int userId))
            {
                return Unauthorized("User ID is missing or invalid.");
            }

            var result = await _userService.DeleteUserAsync(userId);
            if (!result)
                return BadRequest("Failed to delete account.");
            return Ok(new { message = "Account deleted successfully." });
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous] // Allow unauthenticated access for this endpoint
        public async Task<IActionResult> ForgotPassword()
        {
            if (!TryGetUserId(out int userId))
            {
                return Unauthorized("User ID is missing or invalid.");
            }

            var userProfile = await _userService.GetUserProfileAsync(userId, CancellationToken.None);
            if (userProfile == null || string.IsNullOrEmpty(userProfile.Email))
            {
                return BadRequest("User email not found.");
            }

            string otp = GenerateOtp();
            await _userService.StoreOtpAsync(userId, otp);

            string subject = "Password Reset OTP";
            string body = $"Your OTP for password reset is: <strong>{otp}</strong>. It is valid for 10 minutes.";
            await _emailService.SendEmailAsync(userProfile.Email, subject, body);

            return Ok(new { message = "OTP sent to your email. Check your inbox." });
        }

        private string GenerateOtp()
        {
            Random random = new Random();
            return random.Next(100000, 999999).ToString(); 
        }
    }
}