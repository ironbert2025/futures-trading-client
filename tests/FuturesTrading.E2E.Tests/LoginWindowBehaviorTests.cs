using System.Net;
using FlaUI.Core.Tools;

namespace FuturesTrading.E2E.Tests;

/// <summary>
/// Specs 0001-auth-login — AC-4, AC-5, AC-7, AC-8, AC-9 a nivel de VENTANA.
/// La app real se lanza apuntando (Api__BaseUrl) a un backend falso en proceso, así que no
/// dependen del backend real ni de credenciales. Los mensajes exactos y los detalles de la
/// petición ya se verifican en Infrastructure.Tests; aquí se comprueba lo que el usuario VE.
/// </summary>
public class LoginWindowBehaviorTests
{
    private const string MainWindowTitle = "Futures Trading";
    private const string AnyUsername = "aldo";
    private const string AnyPassword = "cualquier-password";
    private const string TokenBody = """{"access_token":"jwt-falso","token_type":"bearer"}""";

    private static void SubmitLogin(AppSession session)
    {
        session.TypeInto("txtUsername", AnyUsername);
        session.TypeInto("txtPassword", AnyPassword);
        session.Click("btnLogin");
    }

    private static void AssertErrorShownAndStillOnLogin(AppSession session, string expectedMessage)
    {
        var error = session.WaitForElement(session.LoginWindow, "lblError");

        Assert.NotNull(error);
        Assert.Equal(expectedMessage, error!.Name);
        Assert.Null(session.FindWindowNow(MainWindowTitle));
    }

    [Fact]
    public void Ac4_BackendUnreachable_ShowsConnectionMessageAndStaysOnLogin()
    {
        using var session = new AppSession(FakeBackend.UnreachableBaseUrl());

        SubmitLogin(session);

        AssertErrorShownAndStillOnLogin(
            session,
            "No se pudo conectar con el servidor. Verifique que la API esté corriendo.");
    }

    [Fact]
    public void Ac5_WhileTheRequestIsInFlight_LoginButtonIsDisabledAndUiResponds_ThenEnabledAgain()
    {
        using var backend = FakeBackend.Returning(
            HttpStatusCode.Unauthorized, """{"detail":"x"}""", delay: TimeSpan.FromSeconds(3));
        using var session = new AppSession(backend.BaseUrl);

        SubmitLogin(session);

        // Mientras el servidor aún no respondió, el botón debe estar deshabilitado. Que la
        // consulta de UI Automation conteste a tiempo demuestra que el hilo de UI no está bloqueado.
        var disabledWhileWaiting = Retry.WhileFalse(
            () => !session.IsEnabled("btnLogin"),
            TimeSpan.FromSeconds(2),
            TimeSpan.FromMilliseconds(100)).Success;
        var serverHadNotAnsweredYet = !backend.ResponseSent;

        Assert.True(disabledWhileWaiting, "El botón Ingresar debe deshabilitarse mientras hay una petición en curso.");
        Assert.True(serverHadNotAnsweredYet, "La comprobación debe ocurrir antes de que el servidor responda.");

        Assert.NotNull(session.WaitForElement(session.LoginWindow, "lblError"));
        Assert.True(session.IsEnabled("btnLogin"), "El botón debe rehabilitarse al terminar.");
    }

    [Fact]
    public void Ac7_ServerError_ShowsAMessageWithTheCodeAndTheAppStaysOpen()
    {
        using var backend = FakeBackend.Returning(HttpStatusCode.InternalServerError, "{}");
        using var session = new AppSession(backend.BaseUrl);

        SubmitLogin(session);

        AssertErrorShownAndStillOnLogin(session, "El servidor respondió con un error (500).");
    }

    [Fact]
    public void Ac9_OkWithoutAToken_ShowsInvalidResponseMessageAndStaysOnLogin()
    {
        using var backend = FakeBackend.Returning(HttpStatusCode.OK, "{}");
        using var session = new AppSession(backend.BaseUrl);

        SubmitLogin(session);

        AssertErrorShownAndStillOnLogin(session, "Respuesta inválida del servidor.");
    }

    [Fact]
    public void Ac8_ClosingTheMainWindow_EndsTheWholeApplication()
    {
        using var backend = FakeBackend.Returning(HttpStatusCode.OK, TokenBody);
        using var session = new AppSession(backend.BaseUrl);

        SubmitLogin(session);
        var mainWindow = session.WaitForWindow(MainWindowTitle);
        Assert.NotNull(mainWindow);

        mainWindow!.Close();

        Assert.True(
            session.WaitForExit(TimeSpan.FromSeconds(5)),
            "Al cerrar la ventana principal el proceso debe terminar (no quedar el login oculto).");
    }
}
