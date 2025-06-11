﻿

using Application.DTOs;
using Application.Interfaces;
using Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace UserManagementAPI.Controllers
{
    [ApiController]
    [Route("UMS/api/Auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IUserRepository _userRepository;
        private readonly IEmailService _emailService;
        private readonly IMemoryCache _memoryCache;

        public AuthController(IAuthService authService, IUserRepository userRepository, IEmailService emailService, IMemoryCache memoryCache)
        {
            _authService = authService;
            _userRepository = userRepository;
            _emailService = emailService;
            _memoryCache = memoryCache;
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                return BadRequest(new { error = "Email is not registered" });
            }

            var otp = GenerateSecureOtp();
            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
            };
            _memoryCache.Set($"OTP_{request.Email}", new OtpData { Otp = otp, UserId = user.Id }, cacheEntryOptions);

            try
            {
                await _emailService.SendEmailAsync(user.Email, "Password Reset Code",
                    $"Your OTP is: <b>{otp}</b>. It will expire in 10 minutes.");
            }
            catch
            {
                return StatusCode(500, new { error = "Failed to send email. Please try again later." });
            }

            return Ok(new { message = "OTP has been sent to your email" });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            if (!_memoryCache.TryGetValue($"OTP_{request.Email}", out OtpData? otpData) || otpData == null || otpData.Otp != request.Otp)
            {
                return BadRequest(new { error = "Invalid or expired OTP" });
            }

            var user = await _userRepository.GetByIdAsync(otpData.UserId);
            if (user == null)
            {
                return BadRequest(new { error = "User not found" });
            }

            user.PasswordHash = Utilities.Security.PasswordHelper.HashPassword(request.NewPassword);
            await _userRepository.UpdateAsync(user);
            _memoryCache.Remove($"OTP_{request.Email}");

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

}