using Application.DTOs;
using Application.Interfaces;
using Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using UserManagementAPI.Models;
using Utilities.Security;
using Domain.Entities;
using Microsoft.AspNetCore.Identity.Data;


namespace UserManagementAPI.Controllers
{
    [ApiController]
    [Route("api/v1/ums/auth")] // Added alternate route for inter-service communication
    public class AuthController : ControllerBase
    {
        private readonly IGoogleAuthService _googleAuthService;

        private readonly IAuthService _authService;
        private readonly IUserRepository _userRepository;
        private readonly IEmailService _emailService;
        private readonly IMemoryCache _memoryCache;
        private readonly IUserService _userService;


        public AuthController(IAuthService authService, IUserService userService, IUserRepository userRepository, IEmailService emailService, IMemoryCache memoryCache, IGoogleAuthService googleAuthService)
        {
            _authService = authService;
            _userRepository = userRepository;
            _emailService = emailService;
            _memoryCache = memoryCache;
            _googleAuthService = googleAuthService;
            _userService = userService ?? throw new ArgumentNullException(nameof(userService));

        }

        // Add this to your AuthController.cs file

        [HttpPost("google-login")]
        [AllowAnonymous]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            if (string.IsNullOrEmpty(request.IdToken))
            {
                return BadRequest(new { error = "ID token is required" });
            }

            try
            {
                GoogleLoginDto googleLoginDto = new GoogleLoginDto
                {
                    IdToken = request.IdToken
                };
                LoginResponseDto response = await _authService.LoginWithGoogleAsync(googleLoginDto);
                if (response == null)
                {
                    return Unauthorized(new { error = "Invalid Google login attempt" });
                }
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while processing the request", details = ex.Message });
            }

        }

        [HttpPost("validate")]
        public async Task<IActionResult> ValidateToken([FromBody] TokenValidationRequest request)
        {

            if (string.IsNullOrEmpty(request.Token))
            {
                return Ok(new TokenValidationResponse { Valid = false });
            }

            try
            {
                var (isValid, principal) = JwtHelper.ValidateToken(request.Token);

                if (!isValid || principal == null)
                {
                    return Ok(new TokenValidationResponse { Valid = false });
                }

                // Extract claims
                var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
                var email = principal.FindFirstValue(ClaimTypes.Email);
                var role = principal.FindFirstValue(ClaimTypes.Role);

                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int id))
                {
                    return Ok(new TokenValidationResponse { Valid = false });
                }

                // Get more user details if needed
                var user = await _userRepository.GetByIdAsync(id);
                if (user == null)
                {
                    return Ok(new TokenValidationResponse { Valid = false });
                }

                return Ok(new TokenValidationResponse
                {
                    Valid = true,
                    User = new UserInfo
                    {
                        Id = user.Id,
                        Email = user.Email,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Role = user.Role
                    }
                });
            }
            catch
            {
                return Ok(new TokenValidationResponse { Valid = false });
            }
        }
  

        [HttpPost("forgot-password")]
        [AllowAnonymous] // Allow unauthenticated access for this endpoint
        public async Task<IActionResult> ForgotPassword([FromBody] Application.DTOs.ForgotPasswordRequest request)
        {   
          
            if (request.Email == null || string.IsNullOrEmpty(request.Email))
            {
                return BadRequest("User email not found.");
            }
            var userProfile = await _userRepository.GetByEmailAsync(request.Email);
            if (userProfile == null)
            {
                return BadRequest("User not found.");
            }   
            string otp = GenerateSecureOtp();

            await _userService.StoreOtpAsync(userProfile.Id, otp);

            string subject = "Password Reset OTP";
            string body = $"Your OTP for password reset is: <strong>{otp}</strong>. It is valid for 10 minutes.";
            await _emailService.SendEmailAsync(userProfile.Email, subject, body);

            return Ok(new { message = "OTP sent to your email. Check your inbox." });
        }

        [HttpPost("reset-password")]
        [AllowAnonymous] // Allow unauthenticated access for consistency
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            // Logic is now encapsulated in the user service to match ForgotPassword
            var success = await _userService.ResetPasswordAsync(request);

            if (!success)
            {
                return BadRequest(new { error = "Invalid or expired OTP." });
            }

            return Ok(new { message = "Password has been updated successfully" });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            try
            {
                var result = await _authService.RegisterAsync(dto);
                return Ok(new { message = result });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                var response = await _authService.LoginAsync(loginDto);
                return Ok(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto refreshTokenRequest)
        {
            try
            {
                var response = await _authService.RefreshTokenAsync(refreshTokenRequest.RefreshToken);
                return Ok(response);
            }
            catch (Exception ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
        }

        private string GenerateSecureOtp()
        {
            using var rng = RandomNumberGenerator.Create();
            byte[] randomBytes = new byte[4];
            rng.GetBytes(randomBytes);
            int otpValue = Math.Abs(BitConverter.ToInt32(randomBytes, 0)) % 1000000;
            return otpValue.ToString("D6");
        }
    }

    public class RefreshTokenRequestDto
    {
        public string RefreshToken { get; set; } = null!;
    }
    public class GoogleLoginRequest
    {
        public string? IdToken { get; set; }
    }


}