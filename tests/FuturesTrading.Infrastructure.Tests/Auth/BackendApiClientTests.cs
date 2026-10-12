using System.Net;
using System.Text;
using FuturesTrading.Infrastructure;
using FuturesTrading.Infrastructure.Auth;
using Microsoft.Extensions.Options;

namespace FuturesTrading.Infrastructure.Tests.Auth;

/// <summary>
/// Specs 0001-auth-login — AC-2, AC-3, AC-4, AC-6, AC-7, AC-9 (nivel Infrastructure).
/// Se usa un HttpMessageHandler falso: no hay red ni backend. Se verifica lo que
/// BackendApiClient ENVÍA (método, URL, cuerpo) y cómo traduce cada respuesta a AuthResult.
/// </summary>
public class BackendApiClientTests
{
    private const string BaseUrl = "http://localhost:8000/";
    private const string Username = "aldo";
    private const string Password = "S3cret-pass-123";

    private static BackendApiClient CreateSut(StubHttpMessageHandler handler) =>
        new(new HttpClient(handler), Options.Create(new ApiSettings { BaseUrl = BaseUrl }));

    private static HttpResponseMessage Json(HttpStatusCode status, string body) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    // ---- AC-2: éxito ----------------------------------------------------------------

    [Fact]
    public async Task Ac2_Success_ReturnsUserWithTheIssuedToken()
    {
        var handler = new StubHttpMessageHandler(_ =>
            Json(HttpStatusCode.OK, """{"access_token":"jwt-123","token_type":"bearer"}"""));

        var result = await CreateSut(handler).LoginAsync(Username, Password);

        Assert.True(result.Success);
        Assert.Equal(Username, result.User!.Username);
        Assert.Equal("jwt-123", result.User.AccessToken);
        Assert.Null(result.ErrorMessage);
    }

    // ---- AC-6: la contraseña va en el cuerpo, nunca en la URL ------------------------

    [Fact]
    public async Task Ac6_SendsPostWithFormBodyAndNeverPutsThePasswordInTheUrl()
    {
        var handler = new StubHttpMessageHandler(_ =>
            Json(HttpStatusCode.OK, """{"access_token":"t","token_type":"bearer"}"""));

        await CreateSut(handler).LoginAsync(Username, Password);

        Assert.Equal(HttpMethod.Post, handler.Method);
        Assert.Equal("http://localhost:8000/auth/login", handler.RequestUri);
        Assert.DoesNotContain(Password, handler.RequestUri);
        Assert.Equal("application/x-www-form-urlencoded", handler.ContentType);
        Assert.Contains($"username={Username}", handler.Body);
        Assert.Contains($"password={Password}", handler.Body);
    }

    // ---- AC-3: credenciales inválidas -------------------------------------------------

    [Fact]
    public async Task Ac3_Unauthorized_ReturnsTheInvalidCredentialsMessage()
    {
        var handler = new StubHttpMessageHandler(_ =>
            Json(HttpStatusCode.Unauthorized, """{"detail":"Usuario o contraseña incorrectos"}"""));

        var result = await CreateSut(handler).LoginAsync(Username, "mala");

        Assert.False(result.Success);
        Assert.Null(result.User);
        Assert.Equal("Usuario o contraseña incorrectos.", result.ErrorMessage);
    }

    // ---- AC-4: backend no alcanzable ---------------------------------------------------

    [Fact]
    public async Task Ac4_Unreachable_ReturnsTheConnectionMessageInsteadOfThrowing()
    {
        var handler = new StubHttpMessageHandler(_ => throw new HttpRequestException("connection refused"));

        var result = await CreateSut(handler).LoginAsync(Username, Password);

        Assert.False(result.Success);
        Assert.Equal(
            "No se pudo conectar con el servidor. Verifique que la API esté corriendo.",
            result.ErrorMessage);
    }

    // ---- AC-7: error del servidor o timeout -------------------------------------------

    [Fact]
    public async Task Ac7_ServerError_ReturnsAMessageWithTheStatusCode()
    {
        var handler = new StubHttpMessageHandler(_ => Json(HttpStatusCode.InternalServerError, "{}"));

        var result = await CreateSut(handler).LoginAsync(Username, Password);

        Assert.False(result.Success);
        Assert.Equal("El servidor respondió con un error (500).", result.ErrorMessage);
    }

    [Fact]
    public async Task Ac7_Timeout_ReturnsTheTimeoutMessage()
    {
        // HttpClient señala un timeout con TaskCanceledException aunque nadie haya cancelado.
        var handler = new StubHttpMessageHandler(_ => throw new TaskCanceledException("timeout"));

        var result = await CreateSut(handler).LoginAsync(Username, Password);

        Assert.False(result.Success);
        Assert.Equal("La solicitud tardó demasiado. Intente nuevamente.", result.ErrorMessage);
    }

    // ---- AC-9: 200 sin token utilizable -----------------------------------------------

    [Fact]
    public async Task Ac9_OkWithoutAToken_ReturnsTheInvalidResponseMessage()
    {
        var handler = new StubHttpMessageHandler(_ => Json(HttpStatusCode.OK, "{}"));

        var result = await CreateSut(handler).LoginAsync(Username, Password);

        Assert.False(result.Success);
        Assert.Equal("Respuesta inválida del servidor.", result.ErrorMessage);
    }

    /// <summary>
    /// Captura lo enviado DENTRO de SendAsync: el HttpClient puede liberar el request
    /// (y su contenido) al terminar, así que leerlo después no es fiable.
    /// </summary>
    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
        : HttpMessageHandler
    {
        public HttpMethod? Method { get; private set; }
        public string RequestUri { get; private set; } = string.Empty;
        public string? ContentType { get; private set; }
        public string Body { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Method = request.Method;
            RequestUri = request.RequestUri!.ToString();
            ContentType = request.Content?.Headers.ContentType?.MediaType;
            Body = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);

            return respond(request);
        }
    }
}
