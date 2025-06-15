using Google.Apis.Auth;
using System.Threading.Tasks;

namespace Utilities.Security;

public class GoogleTokenValidator
{
    private readonly string _googleClientId;

    public GoogleTokenValidator(string googleClientId)
    {
        _googleClientId = googleClientId;
    }

    public async Task<(bool success, string? name, string? email)> VerifyGoogleTokenAsync(string idToken)
    {
        try
        {
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _googleClientId }
            });

            return (true, payload.Name, payload.Email);
        }
        catch (Exception)
        {
            return (false, null, null);
        }
    }
}

