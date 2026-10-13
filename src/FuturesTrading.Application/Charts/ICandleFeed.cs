using FuturesTrading.Domain;

namespace FuturesTrading.Application.Charts;

/// <summary>
/// Fuente de velas y precios en vivo. La implementación real (Infrastructure) se conecta al hub
/// de OptionsTrader; Application y UI solo conocen esta abstracción.
/// Una misma instancia se comparte entre todos los gráficos: cada uno filtra por su símbolo.
/// </summary>
public interface ICandleFeed
{
    /// <summary>Vela de 1 minuto: símbolo y vela (tiempo en UTC real).</summary>
    event Action<string, Candle>? CandleReceived;

    /// <summary>Último precio operado: símbolo, precio, instante UTC.</summary>
    event Action<string, decimal, DateTime>? LevelOneTickReceived;

    /// <summary>Snapshot de velas ya agrupadas (el hub lo envía al conectarse).</summary>
    event Action<CandlesSnapshot>? CandlesSnapshotReceived;

    /// <summary>Cambio de estado de la conexión: conectado o no, y un texto legible.</summary>
    event Action<bool, string>? ConnectionChanged;

    /// <summary>
    /// Último estado de conexión notificado, o null si aún no hubo ninguno. Igual que los snapshots:
    /// un gráfico que se suscribe tarde no vio el evento y necesita poder consultarlo.
    /// </summary>
    (bool Connected, string Message)? LastConnectionState { get; }

    /// <summary>
    /// Último snapshot recibido para el símbolo, o null. Existe porque el hub envía los snapshots
    /// de todos los símbolos justo al conectar, y un gráfico que termina de inicializarse después
    /// los habría perdido.
    /// </summary>
    CandlesSnapshot? GetLastSnapshot(string symbol);

    /// <summary>Empieza a conectar (y a reconectar indefinidamente). No bloquea ni lanza si el hub no está.</summary>
    void Start();
}
