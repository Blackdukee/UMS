using Application.DTOs;
using Domain.Entities;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IAuthService
    {
        Task<string> RegisterAsync(RegisterDto dto);
        Task<LoginResponseDto> LoginAsync(LoginDto loginDto);
        Task<LoginResponseDto> RefreshTokenAsync(string refreshToken);
        Task<LoginResponseDto> LoginWithGoogleAsync(GoogleLoginDto idToken);
    }
}