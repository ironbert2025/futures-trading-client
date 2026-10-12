namespace FuturesTrading.E2E.Tests;

/// <summary>
/// Specs 0001-auth-login — AC-1, AC-2, AC-3. E2E: UI real WinForms -> backend real
/// (futures-trading-backend en http://localhost:8000) -> base de datos.
///   - AC-1 solo necesita la UI compilada (no backend ni credenciales).
///   - AC-2 y AC-3 requieren el backend levantado (uvicorn local o docker compose) y las
///     variables de entorno E2E_USERNAME y E2E_PASSWORD con un usuario ya registrado.
/// Las credenciales NO viven en el repo.
/// </summary>
public class LoginFlowTests
{
    private const string LoginWindowTitle = "Futures Trading — Ingresar";
    private const string MainWindowTitle = "Futures Trading";

    [Fact]
    public void Ac1_Startup_ShowsLoginWindowWithMaskedPasswordAndLoginButton()
    {
        using var session = new AppSession();

        Assert.Equal(LoginWindowTitle, session.LoginWindow.Title);
        Assert.NotNull(session.WaitForElement(session.LoginWindow, "txtUsername"));

        var password = session.WaitForElement(session.LoginWindow, "txtPassword");
        Assert.NotNull(password);
        Assert.True(password!.Properties.IsPassword.ValueOrDefault, "El campo Password debe estar enmascarado.");

        var loginButton = session.WaitForElement(session.LoginWindow, "btnLogin");
        Assert.NotNull(loginButton);
        Assert.Equal("Ingresar", loginButton!.Name);
    }

    [Fact]
    public void Ac2_Login_WithValidCredentials_ShowsMainWindowWithWelcome()
    {
        var (username, password) = ReadCredentials();
        using var session = new AppSession();
        Assert.Equal(LoginWindowTitle, session.LoginWindow.Title);

        session.TypeInto("txtUsername", username);
        session.TypeInto("txtPassword", password);
        session.Click("btnLogin");

        var mainWindow = session.WaitForWindow(MainWindowTitle);
        Assert.NotNull(mainWindow);

        var welcome = session.WaitForElement(mainWindow!, "lblWelcome");
        Assert.NotNull(welcome);
        Assert.Equal($"Bienvenido, {username}", welcome!.Name);
    }

    [Fact]
    public void Ac3_Login_WithWrongPassword_ShowsErrorAndStaysOnLogin()
    {
        var (username, _) = ReadCredentials();
        using var session = new AppSession();

        session.TypeInto("txtUsername", username);
        session.TypeInto("txtPassword", "definitely-not-the-password");
        session.Click("btnLogin");

        var error = session.WaitForElement(session.LoginWindow, "lblError");
        Assert.NotNull(error);
        Assert.Contains("incorrectos", error!.Name);
        Assert.Null(session.FindWindowNow(MainWindowTitle));
    }

    private static (string Username, string Password) ReadCredentials()
    {
        var username = Environment.GetEnvironmentVariable("E2E_USERNAME");
        var password = Environment.GetEnvironmentVariable("E2E_PASSWORD");

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            throw new InvalidOperationException(
                "Definir las variables de entorno E2E_USERNAME y E2E_PASSWORD con un usuario ya registrado en el backend.");
        }

        return (username, password);
    }
}
