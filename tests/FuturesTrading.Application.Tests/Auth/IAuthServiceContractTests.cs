using FuturesTrading.Application.Auth;
using FuturesTrading.Domain;

namespace FuturesTrading.Application.Tests.Auth;

/// <summary>
/// No hay un caso de uso separado todavía (LoginForm llama IAuthService directamente) — estos
/// tests documentan el contrato esperado de cualquier implementación de IAuthService usando un
/// doble de prueba, sirviendo de base para cuando BackendApiClient tenga sus propios tests de
/// integración en Infrastructure.Tests.
/// </summary>
public class IAuthServiceContractTests
{
    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsSuccessResult()
    {
        IAuthService sut = new FakeAuthService(shouldSucceed: true);

        var result = await sut.LoginAsync("aldo", "correct-password");

        Assert.True(result.Success);
        Assert.Equal("aldo", result.User?.Username);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidCredentials_ReturnsFailureResult()
    {
        IAuthService sut = new FakeAuthService(shouldSucceed: false);

        var result = await sut.LoginAsync("aldo", "wrong-password");

        Assert.False(result.Success);
        Assert.False(string.IsNullOrWhiteSpace(result.ErrorMessage));
    }

    private sealed class FakeAuthService(bool shouldSucceed) : IAuthService
    {
        public Task<AuthResult> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
        {
            var result = shouldSucceed
                ? AuthResult.Ok(new AuthenticatedUser { Username = username, AccessToken = "fake-token" })
                : AuthResult.Fail("Usuario o contraseña incorrectos.");

            return Task.FromResult(result);
        }
    }
}
