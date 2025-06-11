
using Application.DTOs;

public class ResetPasswordRequest
{
    public required string Email { get; set; }
    public required string Otp { get; set; }
    public required string NewPassword { get; set; }
}