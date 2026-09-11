namespace FuturesTrading.Domain;

public sealed class AuthenticatedUser
{
    public required string Username { get; init; }
    public required string AccessToken { get; init; }
}
