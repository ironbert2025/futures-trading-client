namespace FuturesTrading.Domain;

/// <summary>Una vela OHLC. <see cref="Time"/> es el inicio del periodo, en UTC real.</summary>
public sealed record Candle(DateTime Time, decimal Open, decimal High, decimal Low, decimal Close);
