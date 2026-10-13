using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using FuturesTrading.Application.Charts;
using FuturesTrading.Domain;
using Microsoft.Extensions.Options;

namespace FuturesTrading.Infrastructure.Hub;

/// <summary>
/// Cliente del hub de OptionsTrader. Ese programa abre la única conexión WebSocket permitida a
/// Schwab y reenvía cada vela/tick por un socket TCP (puerto 51919 por defecto) como JSON, una
/// línea por mensaje. Esta clase se conecta a ese socket y re-emite los datos como eventos.
///
/// Mensajes (campo "type"; si falta se asume "candle"):
///   candle    {symbol, time(ms UTC), open, high, low, close}   vela de 1 minuto
///   candles15 {symbol, interval, candles[], forming}            snapshot de velas ya agrupadas
///                                                               (cada una: time(ms UTC), open, high,
///                                                               low, close, volume); se envía al conectar
///   l1        {symbol, time(ms UTC), price}                     último precio
///   heartbeat {}                                                el hub sigue vivo (cada 20 s)
///   wsevent   {text}                                            estado de la conexión del hub con Schwab
///
/// Si el hub no está, reintenta cada 5 s indefinidamente. Un watchdog fuerza la reconexión si no
/// llega ninguna línea en 45 s: un TcpClient sin keep-alive no detecta por sí solo una conexión muerta.
/// </summary>
public sealed class CandleHubClient : ICandleFeed, IDisposable
{
    private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan WatchdogInterval = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan StaleThreshold = TimeSpan.FromSeconds(45);

    private readonly HubSettings _settings;
    private readonly CancellationTokenSource _lifetime = new();
    private long _lastLineUtcTicks = DateTime.UtcNow.Ticks;
    private int _started;

    public event Action<string, Candle>? CandleReceived;
    public event Action<string, decimal, DateTime>? LevelOneTickReceived;
    public event Action<CandlesSnapshot>? CandlesSnapshotReceived;
    public event Action<bool, string>? ConnectionChanged;

    private readonly ConcurrentDictionary<string, CandlesSnapshot> _lastSnapshots = new(StringComparer.OrdinalIgnoreCase);

    public CandlesSnapshot? GetLastSnapshot(string symbol) =>
        _lastSnapshots.TryGetValue(symbol, out var snapshot) ? snapshot : null;

    // Referencia (no struct) para que la lectura/escritura entre hilos sea atómica.
    private volatile Tuple<bool, string>? _lastConnection;

    public (bool Connected, string Message)? LastConnectionState =>
        _lastConnection is { } state ? (state.Item1, state.Item2) : null;

    public CandleHubClient(IOptions<HubSettings> settings)
    {
        _settings = settings.Value;
    }

    public void Start()
    {
        if (Interlocked.Exchange(ref _started, 1) == 1) return;
        _ = Task.Run(() => RunAsync(_lifetime.Token));
    }

    private async Task RunAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await ConnectAndReadAsync(ct);
                RaiseConnectionChanged(false, "El hub cerró la conexión. Reintentando...");
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                RaiseConnectionChanged(false, $"Sin conexión con el hub {_settings.Host}:{_settings.Port} ({ex.Message}). Reintentando...");
            }

            try
            {
                await Task.Delay(ReconnectDelay, ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private async Task ConnectAndReadAsync(CancellationToken ct)
    {
        using var client = new TcpClient();
        await client.ConnectAsync(_settings.Host, _settings.Port, ct);

        MarkLineReceived();
        RaiseConnectionChanged(true, $"Conectado al hub {_settings.Host}:{_settings.Port}");

        using var connection = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var watchdog = WatchdogAsync(client, connection.Token);

        try
        {
            using var reader = new StreamReader(client.GetStream(), Encoding.UTF8);
            while (true)
            {
                var line = await reader.ReadLineAsync(ct);
                if (line is null) return; // el hub cerró la conexión
                MarkLineReceived();
                HandleLine(line);
            }
        }
        finally
        {
            connection.Cancel();
            await watchdog;
        }
    }

    private async Task WatchdogAsync(TcpClient client, CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(WatchdogInterval, ct);

                var sinceLastLine = DateTime.UtcNow - new DateTime(Interlocked.Read(ref _lastLineUtcTicks), DateTimeKind.Utc);
                if (sinceLastLine <= StaleThreshold) continue;

                RaiseConnectionChanged(false, "El hub no envía datos desde hace demasiado. Reconectando...");
                client.Dispose(); // hace que ReadLineAsync lance y se reconecte desde RunAsync
                return;
            }
        }
        catch (OperationCanceledException)
        {
            // La conexión terminó por su cuenta.
        }
    }

    private void MarkLineReceived() => Interlocked.Exchange(ref _lastLineUtcTicks, DateTime.UtcNow.Ticks);

    private void HandleLine(string line)
    {
        try
        {
            using var doc = JsonDocument.Parse(line);
            var root = doc.RootElement;

            var type = root.TryGetProperty("type", out var typeElement) ? typeElement.GetString() : "candle";

            if (type == "heartbeat") return;

            if (type == "wsevent")
            {
                RaiseConnectionChanged(true, root.GetProperty("text").GetString() ?? string.Empty);
                return;
            }

            var symbol = root.GetProperty("symbol").GetString();
            if (string.IsNullOrEmpty(symbol)) return;

            if (type == "candles15")
            {
                HandleSnapshot(symbol, root);
                return;
            }

            var time = DateTimeOffset.FromUnixTimeMilliseconds(root.GetProperty("time").GetInt64()).UtcDateTime;

            if (type == "l1")
            {
                RaiseLevelOneTick(symbol, root.GetProperty("price").GetDecimal(), time);
                return;
            }

            RaiseCandle(symbol, new Candle(
                time,
                root.GetProperty("open").GetDecimal(),
                root.GetProperty("high").GetDecimal(),
                root.GetProperty("low").GetDecimal(),
                root.GetProperty("close").GetDecimal()));
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
        {
            Debug.WriteLine($"CandleHubClient: línea ignorada ({ex.GetType().Name}): {line}");
        }
    }

    private void HandleSnapshot(string symbol, JsonElement root)
    {
        var interval = root.TryGetProperty("interval", out var intervalElement) ? intervalElement.GetInt32() : 15;

        var closed = new List<Candle>();
        if (root.TryGetProperty("candles", out var candlesElement) && candlesElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in candlesElement.EnumerateArray())
                closed.Add(ParseSnapshotCandle(item));
        }

        Candle? forming = null;
        if (root.TryGetProperty("forming", out var formingElement) && formingElement.ValueKind == JsonValueKind.Object)
            forming = ParseSnapshotCandle(formingElement);

        closed.Sort((a, b) => a.Time.CompareTo(b.Time));

        var snapshot = new CandlesSnapshot(symbol, interval, closed, forming);
        _lastSnapshots[symbol] = snapshot;

        var handlers = CandlesSnapshotReceived;
        if (handlers is null) return;
        foreach (var handler in handlers.GetInvocationList().Cast<Action<CandlesSnapshot>>())
        {
            try { handler(snapshot); }
            catch (Exception ex) { Debug.WriteLine($"CandleHubClient: suscriptor de snapshot falló: {ex}"); }
        }
    }

    private static Candle ParseSnapshotCandle(JsonElement element) => new(
        DateTimeOffset.FromUnixTimeMilliseconds(element.GetProperty("time").GetInt64()).UtcDateTime,
        element.GetProperty("open").GetDecimal(),
        element.GetProperty("high").GetDecimal(),
        element.GetProperty("low").GetDecimal(),
        element.GetProperty("close").GetDecimal());

    // Cada suscriptor se invoca por separado: si uno lanza, no deja sin datos a los siguientes.
    private void RaiseCandle(string symbol, Candle candle)
    {
        var handlers = CandleReceived;
        if (handlers is null) return;
        foreach (var handler in handlers.GetInvocationList().Cast<Action<string, Candle>>())
        {
            try { handler(symbol, candle); }
            catch (Exception ex) { Debug.WriteLine($"CandleHubClient: suscriptor de vela falló: {ex}"); }
        }
    }

    private void RaiseLevelOneTick(string symbol, decimal price, DateTime utcTime)
    {
        var handlers = LevelOneTickReceived;
        if (handlers is null) return;
        foreach (var handler in handlers.GetInvocationList().Cast<Action<string, decimal, DateTime>>())
        {
            try { handler(symbol, price, utcTime); }
            catch (Exception ex) { Debug.WriteLine($"CandleHubClient: suscriptor de tick falló: {ex}"); }
        }
    }

    private void RaiseConnectionChanged(bool connected, string message)
    {
        _lastConnection = Tuple.Create(connected, message);

        var handlers = ConnectionChanged;
        if (handlers is null) return;
        foreach (var handler in handlers.GetInvocationList().Cast<Action<bool, string>>())
        {
            try { handler(connected, message); }
            catch (Exception ex) { Debug.WriteLine($"CandleHubClient: suscriptor de estado falló: {ex}"); }
        }
    }

    public void Dispose()
    {
        _lifetime.Cancel();
        _lifetime.Dispose();
    }
}
