namespace FuturesTrading.E2E.Tests;

/// <summary>
/// E2E del flujo de login: UI real WinForms -> backend real (futures-trading-backend corriendo en
/// http://localhost:8000) -> base de datos. Requisitos antes de correr:
///   1. El backend debe estar levantado (uvicorn local o docker compose).
///   2. Variables de entorno E2E_USERNAME y E2E_PASSWORD con un usuario válido ya registrado.
/// Las credenciales NO viven en el repo.
/// </summary>
public class LoginFlowTests
{
    private const string LoginWindowTitle = "Futures Trading — Ingresar";
    private const string MainWindowTitle = "Futures Trading";

    [Fact]
    public void Login_WithValidCredentials_ShowsMainWindowWithWelcome()
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
    public void Login_WithWrongPassword_ShowsErrorAndStaysOnLogin()
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
