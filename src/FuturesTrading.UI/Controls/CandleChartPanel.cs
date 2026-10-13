using System.Globalization;
using System.Text.Json;
using FuturesTrading.Application.Charts;
using FuturesTrading.Domain;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace FuturesTrading.UI.Controls;

/// <summary>
/// Gráfico de velas de 15 minutos de UN símbolo, alimentado por el hub de OptionsTrader.
/// El hub entrega un snapshot de velas ya agrupadas al conectar y luego ticks de último precio;
/// con los ticks se actualiza la vela en formación y se abren las siguientes. Se pinta en una
/// página local con Lightweight Charts dentro de un WebView2.
///
/// Varios paneles comparten el mismo <see cref="ICandleFeed"/>; cada uno filtra por su símbolo.
/// Un panel sin símbolo no inicializa WebView2 ni escucha el feed.
/// </summary>
public sealed class CandleChartPanel : UserControl
{
    private const int IntervalMinutes = 15;
    private const string AssetsHost = "chart.local";

    private static readonly JsonSerializerOptions ZoneJson = new(JsonSerializerDefaults.Web);

    private static readonly TimeZoneInfo Eastern = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");

    private readonly ICandleFeed _feed;
    private readonly int _slot;
    private readonly string _symbol;
    private readonly string _sessionLabel;
    private readonly decimal _tickSize;
    private readonly CandleAggregator _aggregator;
    private readonly object _aggregatorLock = new();
    private readonly Label _header = new();
    private readonly WebView2 _webView = new();

    private bool _chartReady;
    private bool _dzSzArmed;
    private CandlesSnapshot? _appliedSnapshot;

    private bool HasSymbol => _symbol.Length > 0;

    /// <param name="slot">Número del panel (1..N), solo para identificarlo en pantalla.</param>
    public CandleChartPanel(ICandleFeed feed, ChartSettings settings, int slot)
    {
        _feed = feed;
        _slot = slot;
        _symbol = (settings.Symbol ?? "").Trim().ToUpperInvariant();
        _sessionLabel = settings.RthOnly ? "RTH" : "24 h";
        _tickSize = settings.TickSize;
        _aggregator = new CandleAggregator(IntervalMinutes, settings.RthOnly);

        _header.Name = $"lblChartStatus{slot}";
        _header.Dock = DockStyle.Top;
        _header.Height = 26;
        _header.TextAlign = ContentAlignment.MiddleLeft;
        _header.Padding = new Padding(8, 0, 0, 0);
        _header.Text = HasSymbol
            ? $"{_symbol} · {IntervalMinutes} min {_sessionLabel} · iniciando..."
            : $"Chart {slot} · sin símbolo (define Charts:Panels:{slot - 1}:Symbol en appsettings.json)";
        _header.ForeColor = HasSymbol ? SystemColors.ControlText : SystemColors.GrayText;

        _webView.Name = $"chartWebView{slot}";
        _webView.Dock = DockStyle.Fill;
        _webView.Visible = HasSymbol;

        BackColor = Color.FromArgb(0x13, 0x17, 0x22);

        Controls.Add(_webView);
        Controls.Add(_header);
    }

    protected override async void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        if (!HasSymbol) return;
        await InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            await _webView.EnsureCoreWebView2Async();

            var assetsFolder = Path.Combine(AppContext.BaseDirectory, "ChartAssets");
            _webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                AssetsHost, assetsFolder, CoreWebView2HostResourceAccessKind.Allow);

            var navigated = new TaskCompletionSource();
            void OnNavigationCompleted(object? s, CoreWebView2NavigationCompletedEventArgs args)
            {
                _webView.CoreWebView2.NavigationCompleted -= OnNavigationCompleted;
                if (args.IsSuccess) navigated.TrySetResult();
                else navigated.TrySetException(new InvalidOperationException($"No se pudo cargar la página del gráfico ({args.WebErrorStatus})."));
            }

            _webView.CoreWebView2.NavigationCompleted += OnNavigationCompleted;
            _webView.CoreWebView2.Navigate($"https://{AssetsHost}/chart.html");
            await navigated.Task;

            // La granularidad se fija antes de que lleguen velas, para que el eje y las líneas
            // DZ/SZ ya usen el tick size del contrato.
            if (_tickSize > 0)
            {
                var tick = _tickSize.ToString(CultureInfo.InvariantCulture);
                await _webView.CoreWebView2.ExecuteScriptAsync($"configureTick({tick});");
            }

            // Zonas DZ/SZ guardadas de este símbolo; las nuevas llegan por WebMessageReceived.
            _webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;
            var savedZones = JsonSerializer.Serialize(ZoneStore.Load(_symbol), ZoneJson);
            await _webView.CoreWebView2.ExecuteScriptAsync($"loadZones({savedZones});");

            _chartReady = true;
            if (_dzSzArmed) PushDzSzArmed();
            _feed.CandleReceived += OnCandleReceived;
            _feed.LevelOneTickReceived += OnLevelOneTickReceived;
            _feed.CandlesSnapshotReceived += ApplySnapshot;
            _feed.ConnectionChanged += OnConnectionChanged;
            _feed.Start();

            // El hub manda los snapshots justo al conectar; si otro panel abrió la conexión antes
            // de que este terminara de inicializarse, el evento ya pasó: se recupera del caché.
            var cached = _feed.GetLastSnapshot(_symbol);
            if (cached is not null) ApplySnapshot(cached);

            // Igual con el estado de conexión: si ya estaba conectado, el evento ya pasó.
            if (_feed.LastConnectionState is { } state) OnConnectionChanged(state.Connected, state.Message);
        }
        catch (WebView2RuntimeNotFoundException)
        {
            ShowError("No se encontró el runtime de WebView2. Instálalo desde https://developer.microsoft.com/microsoft-edge/webview2/");
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.Runtime.InteropServices.COMException)
        {
            ShowError($"No se pudo iniciar el gráfico: {ex.Message}");
        }
    }

    /// <summary>
    /// Arma o desarma el dibujo de zonas DZ/SZ en este gráfico (1er click: línea verde de Demanda,
    /// 2º click: línea roja de Supply). No hace nada si el panel no tiene símbolo o aún no cargó.
    /// </summary>
    public void SetDzSzArmed(bool on)
    {
        if (!HasSymbol) return;

        // Se recuerda el estado: si el gráfico aún no terminó de cargar, se aplica al estar listo.
        _dzSzArmed = on;
        PushDzSzArmed();
    }

    private void PushDzSzArmed()
    {
        var on = _dzSzArmed;
        RunOnUiThread(() =>
        {
            if (!_chartReady || _webView.CoreWebView2 is null) return;
            _ = _webView.CoreWebView2.ExecuteScriptAsync($"setDzSzArmed({(on ? "true" : "false")});");
        });
    }

    // La página manda la lista completa de zonas cada vez que cambia (se completa o se borra una).
    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            var zones = JsonSerializer.Deserialize<List<ZoneDto>>(e.TryGetWebMessageAsString(), ZoneJson);
            if (zones is not null) ZoneStore.Save(_symbol, zones);
        }
        catch (JsonException) { /* mensaje mal formado: se ignora, no se pierde lo ya guardado */ }
    }

    private void OnCandleReceived(string symbol, Candle minuteCandle)
    {
        if (!string.Equals(symbol, _symbol, StringComparison.OrdinalIgnoreCase)) return;

        Candle? updated;
        lock (_aggregatorLock) updated = _aggregator.AddMinuteCandle(minuteCandle);
        if (updated is not null) PushCandle(updated);
    }

    private void OnLevelOneTickReceived(string symbol, decimal price, DateTime utcTime)
    {
        if (!string.Equals(symbol, _symbol, StringComparison.OrdinalIgnoreCase)) return;

        Candle? updated;
        lock (_aggregatorLock) updated = _aggregator.AddTick(price, utcTime);
        if (updated is not null) PushCandle(updated);
    }

    // Snapshot del hub: historial de la sesión + vela en formación. Reemplaza lo que haya y deja
    // al agregador continuando sobre la vela en formación REAL (apertura y extremos correctos).
    private void ApplySnapshot(CandlesSnapshot snapshot)
    {
        if (!string.Equals(snapshot.Symbol, _symbol, StringComparison.OrdinalIgnoreCase)) return;
        if (snapshot.IntervalMinutes != IntervalMinutes) return;

        var all = new List<Candle>(snapshot.Candles);
        lock (_aggregatorLock)
        {
            // Mismo snapshot por el evento y por el caché: no se aplica dos veces (re-sembrar
            // descartaría los ticks que ya se aplicaron en medio).
            if (ReferenceEquals(_appliedSnapshot, snapshot)) return;
            _appliedSnapshot = snapshot;

            if (snapshot.Forming is not null)
            {
                _aggregator.Seed(snapshot.Forming);
                all.Add(snapshot.Forming);
            }
        }

        var json = JsonSerializer.Serialize(all.Select(ToChartPoint));

        RunOnUiThread(() =>
        {
            if (!_chartReady || _webView.CoreWebView2 is null) return;
            _ = _webView.CoreWebView2.ExecuteScriptAsync($"loadHistory({json});");
        });
    }

    private void OnConnectionChanged(bool connected, string message)
    {
        RunOnUiThread(() =>
        {
            _header.ForeColor = connected ? Color.SeaGreen : Color.Firebrick;
            _header.Text = $"{_symbol} · {IntervalMinutes} min {_sessionLabel} · {message}";
        });
    }

    private void PushCandle(Candle candle)
    {
        var json = JsonSerializer.Serialize(ToChartPoint(candle));

        RunOnUiThread(() =>
        {
            if (!_chartReady || _webView.CoreWebView2 is null) return;
            _ = _webView.CoreWebView2.ExecuteScriptAsync($"upsertCandle({json});");
        });
    }

    private static object ToChartPoint(Candle c) => new
    {
        time = FakeUtcEpochSeconds(c.Time),
        open = c.Open,
        high = c.High,
        low = c.Low,
        close = c.Close,
    };

    // Lightweight Charts dibuja el timestamp como dígitos UTC literales, sin convertir a la zona
    // del equipo. Para que el eje muestre hora de Nueva York sin importar la zona de la PC, se
    // convierte el instante a hora de pared de NY y ESA hora se marca como si fuera UTC.
    private static long FakeUtcEpochSeconds(DateTime utcTime)
    {
        var eastern = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utcTime, DateTimeKind.Utc), Eastern);
        return new DateTimeOffset(DateTime.SpecifyKind(eastern, DateTimeKind.Utc)).ToUnixTimeSeconds();
    }

    private void ShowError(string message)
    {
        _webView.Visible = false;
        _header.ForeColor = Color.Firebrick;
        _header.Text = $"Chart {_slot} · {message}";
    }

    // Los eventos del hub llegan desde un hilo de fondo; los controles solo se tocan en el de UI.
    private void RunOnUiThread(Action action)
    {
        if (IsDisposed || !IsHandleCreated) return;
        try { BeginInvoke(action); }
        catch (InvalidOperationException) { /* el formulario se está cerrando */ }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _feed.CandleReceived -= OnCandleReceived;
            _feed.LevelOneTickReceived -= OnLevelOneTickReceived;
            _feed.CandlesSnapshotReceived -= ApplySnapshot;
            _feed.ConnectionChanged -= OnConnectionChanged;
        }

        base.Dispose(disposing);
    }
}
