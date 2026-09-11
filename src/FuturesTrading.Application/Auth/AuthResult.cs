using FuturesTrading.Domain;

namespace FuturesTrading.Application.Auth;

public sealed class AuthResult
{
    public bool Success { get; }
    public AuthenticatedUser? User { get; }
    public string? ErrorMessage { get; }

    private AuthResult(bool success, AuthenticatedUser? user, string? errorMessage)
    {
        Success = success;
        User = user;
        ErrorMessage = errorMessage;
    }

    public static AuthResult Ok(AuthenticatedUser user) => new(true, user, null);

    public static AuthResult Fail(string errorMessage) => new(false, null, errorMessage);
}
