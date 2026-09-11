using FuturesTrading.Application.Auth;
using FuturesTrading.Domain;

namespace FuturesTrading.Application.Tests.Auth;

public class AuthResultTests
{
    [Fact]
    public void Ok_SetsSuccessAndUser_NoErrorMessage()
    {
        var user = new AuthenticatedUser { Username = "aldo", AccessToken = "token123" };

        var result = AuthResult.Ok(user);

        Assert.True(result.Success);
        Assert.Same(user, result.User);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void Fail_SetsErrorMessage_NoUser()
    {
        var result = AuthResult.Fail("Usuario o contraseña incorrectos.");

        Assert.False(result.Success);
        Assert.Null(result.User);
        Assert.Equal("Usuario o contraseña incorrectos.", result.ErrorMessage);
    }
}
