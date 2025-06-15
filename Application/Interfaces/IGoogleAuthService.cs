using System.Threading.Tasks;


namespace Application.Interfaces;

public interface IGoogleAuthService
{

    Task<(bool success, string? name, string? email)> VerifyGoogleTokenAsync(string idToken);
}

