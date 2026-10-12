using System.Net;
using System.Net.Sockets;
using System.Text;

namespace FuturesTrading.E2E.Tests;

/// <summary>
/// Backend falso en proceso (HttpListener) para E2E deterministas: responde siempre lo mismo,
/// opcionalmente después de una espera. La app real se apunta a él con Api__BaseUrl.
/// </summary>
public sealed class FakeBackend : IDisposable
{
    private readonly HttpListener _listener = new();
    private volatile bool _responseSent;

    public string BaseUrl { get; }

    /// <summary>True cuando el servidor ya terminó de escribir la respuesta.</summary>
    public bool ResponseSent => _responseSent;

    private FakeBackend(HttpStatusCode status, string body, TimeSpan delay)
    {
        BaseUrl = $"http://localhost:{FindFreePort()}/";
        _listener.Prefixes.Add(BaseUrl);
        _listener.Start();
        _ = Task.Run(() => ServeAsync(status, body, delay));
    }

    public static FakeBackend Returning(HttpStatusCode status, string body, TimeSpan? delay = null) =>
        new(status, body, delay ?? TimeSpan.Zero);

    /// <summary>URL de un puerto en el que no escucha nadie (simula backend caído).</summary>
    public static string UnreachableBaseUrl() => $"http://localhost:{FindFreePort()}/";

    private async Task ServeAsync(HttpStatusCode status, string body, TimeSpan delay)
    {
        while (_listener.IsListening)
        {
            HttpListenerContext context;
            try
            {
                context = await _listener.GetContextAsync();
            }
            catch (Exception) when (!_listener.IsListening)
            {
                return;
            }

            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay);
            }

            var bytes = Encoding.UTF8.GetBytes(body);
            context.Response.StatusCode = (int)status;
            context.Response.ContentType = "application/json";
            context.Response.ContentLength64 = bytes.Length;
            await context.Response.OutputStream.WriteAsync(bytes);
            context.Response.Close();
            _responseSent = true;
        }
    }

    private static int FindFreePort()
    {
        var probe = new TcpListener(IPAddress.Loopback, 0);
        probe.Start();
        var port = ((IPEndPoint)probe.LocalEndpoint).Port;
        probe.Stop();
        return port;
    }

    public void Dispose()
    {
        _listener.Close();
    }
}
