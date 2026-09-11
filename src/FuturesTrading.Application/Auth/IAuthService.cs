namespace FuturesTrading.Application.Auth;

/// <summary>
/// Autenticación contra el backend (futures-trading-backend). La implementación real
/// vive en Infrastructure — Application solo conoce esta abstracción.
/// </summary>
public interface IAuthService
{
    Task<AuthResult> LoginAsync(string username, string password, CancellationToken cancellationToken = default);
}
