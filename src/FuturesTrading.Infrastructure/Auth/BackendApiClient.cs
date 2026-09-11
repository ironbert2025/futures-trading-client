using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using FuturesTrading.Application.Auth;
using FuturesTrading.Domain;
using Microsoft.Extensions.Options;

namespace FuturesTrading.Infrastructure.Auth;

/// <summary>
/// Implementación real de IAuthService: habla por HTTP con futures-trading-backend.
/// El backend expone /auth/login como OAuth2 password flow (form-urlencoded), no JSON,
/// para poder usar el botón "Authorize" de Swagger tal cual viene de FastAPI.
/// </summary>
public sealed class BackendApiClient : IAuthService
{
    private readonly HttpClient _httpClient;

    public BackendApiClient(HttpClient httpClient, IOptions<ApiSettings> apiSettings)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(apiSettings.Value.BaseUrl);
    }

    public async Task<AuthResult> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var form = new Dictionary<string, string>
        {
            ["username"] = username,
            ["password"] = password,
        };

        try
        {
            using var response = await _httpClient.PostAsync(
                "auth/login",
                new FormUrlEncodedContent(form),
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return AuthResult.Fail("Usuario o contraseña incorrectos.");
            }

            if (!response.IsSuccessStatusCode)
            {
                return AuthResult.Fail($"El servidor respondió con un error ({(int)response.StatusCode}).");
            }

            var token = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken: cancellationToken);
            if (token is null || string.IsNullOrWhiteSpace(token.AccessToken))
            {
                return AuthResult.Fail("Respuesta inválida del servidor.");
            }

            var user = new AuthenticatedUser { Username = username, AccessToken = token.AccessToken };
            return AuthResult.Ok(user);
        }
        catch (HttpRequestException)
        {
            return AuthResult.Fail("No se pudo conectar con el servidor. Verifique que la API esté corriendo.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return AuthResult.Fail("La solicitud tardó demasiado. Intente nuevamente.");
        }
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = string.Empty;
    }
}
