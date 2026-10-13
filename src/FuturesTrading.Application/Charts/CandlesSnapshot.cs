using FuturesTrading.Domain;

namespace FuturesTrading.Application.Charts;

/// <summary>
/// Velas de un símbolo ya agrupadas por la fuente (el hub las entrega al conectarse): las cerradas
/// en orden cronológico y, si existe, la que sigue en formación.
/// </summary>
public sealed record CandlesSnapshot(
    string Symbol,
    int IntervalMinutes,
    IReadOnlyList<Candle> Candles,
    Candle? Forming);
