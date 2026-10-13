namespace FuturesTrading.E2E.Tests;

/// <summary>
/// Specs 0002-home-startup — AC-1 y AC-2. No necesitan backend ni credenciales:
/// al arrancar, la app no llama a ningún servicio.
/// </summary>
public class HomeWindowTests
{
    private const string HomeWindowTitle = "Futures Trading — Home";
    private const string LoginWindowTitle = "Futures Trading — Ingresar";

    [Fact]
    public void Ac1_Startup_ShowsTheHomeWindow_AndNotTheLogin()
    {
        using var session = new AppSession();

        Assert.Equal(HomeWindowTitle, session.StartupWindow.Title);
        Assert.Null(session.FindWindowNow(LoginWindowTitle));
    }

    [Fact]
    public void Ac2_ClosingHome_EndsTheApplication()
    {
        using var session = new AppSession();

        session.StartupWindow.Close();

        Assert.True(
            session.WaitForExit(TimeSpan.FromSeconds(5)),
            "Al cerrar Home el proceso debe terminar.");
    }
}
