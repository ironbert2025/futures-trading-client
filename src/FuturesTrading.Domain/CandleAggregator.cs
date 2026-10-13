namespace FuturesTrading.Domain;

/// <summary>
/// Agrupa velas de 1 minuto y ticks de último precio en velas de N minutos.
///
/// Dos modos de sesión:
///   rthOnly = true   sesión regular de acciones, 9:30–16:00 hora de Nueva York; los periodos se
///                    anclan a las 9:30 ET, igual que el panel "15m RTH" de OptionsTrader.
///   rthOnly = false  24 horas (futuros); los periodos se alinean al reloj (:00, :15, :30, :45).
///
/// Los ticks pueden abrir un periodo nuevo: hace falta cuando la fuente solo entrega Level 1 y no
/// barras de 1 minuto (como el hub con futuros). Si llegan ambas cosas, se combinan: el máximo y
/// el mínimo son los extremos de las dos fuentes y el cierre es el último dato recibido.
///
/// Solo mantiene el periodo en formación. No es seguro entre hilos: quien lo use debe serializar
/// el acceso (el hub entrega todo desde un único hilo de lectura).
/// </summary>
public sealed class CandleAggregator
{
    private static readonly TimeZoneInfo Eastern = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
    private static readonly TimeSpan SessionOpen = new(9, 30, 0);
    private static readonly TimeSpan SessionClose = new(16, 0, 0);

    private readonly int _intervalMinutes;
    private readonly bool _rthOnly;

    private DateTime _bucketStartUtc;
    private decimal _open, _high, _low, _close;
    private bool _hasBucket;

    /// <param name="intervalMinutes">Duración del periodo. Con rthOnly = false debe dividir 60 (1, 5, 15, 30, 60).</param>
    public CandleAggregator(int intervalMinutes, bool rthOnly)
    {
        if (intervalMinutes <= 0) throw new ArgumentOutOfRangeException(nameof(intervalMinutes));
        if (!rthOnly && 60 % intervalMinutes != 0) throw new ArgumentOutOfRangeException(nameof(intervalMinutes), "En modo 24 h el intervalo debe dividir 60.");

        _intervalMinutes = intervalMinutes;
        _rthOnly = rthOnly;
    }

    /// <summary>
    /// Incorpora una vela de 1 minuto. Devuelve la vela del periodo actualizada, o null si el
    /// minuto cae fuera de la sesión o pertenece a un periodo anterior al que está en formación.
    /// </summary>
    public Candle? AddMinuteCandle(Candle minute)
    {
        var bucketStart = BucketStartUtc(minute.Time);
        if (bucketStart is null) return null;

        if (!_hasBucket || bucketStart.Value > _bucketStartUtc)
        {
            StartBucket(bucketStart.Value, minute.Open, minute.High, minute.Low, minute.Close);
            return Current;
        }

        if (bucketStart.Value < _bucketStartUtc) return null;

        _high = Math.Max(_high, minute.High);
        _low = Math.Min(_low, minute.Low);
        _close = minute.Close;
        return Current;
    }

    /// <summary>
    /// Aplica un tick de último precio. Si pertenece al periodo en formación, extiende máximo/mínimo
    /// y fija el cierre; si pertenece a uno posterior (o aún no hay periodo), lo abre con ese precio.
    /// Un tick de un periodo ya cerrado se descarta.
    /// </summary>
    public Candle? AddTick(decimal price, DateTime utcTime)
    {
        if (price <= 0) return null;

        var bucketStart = BucketStartUtc(utcTime);
        if (bucketStart is null) return null;

        if (!_hasBucket || bucketStart.Value > _bucketStartUtc)
        {
            StartBucket(bucketStart.Value, price, price, price, price);
            return Current;
        }

        if (bucketStart.Value < _bucketStartUtc) return null;

        _high = Math.Max(_high, price);
        _low = Math.Min(_low, price);
        _close = price;
        return Current;
    }

    /// <summary>
    /// Fija la vela en formación a partir de una fuente que ya la conoce completa (el snapshot del
    /// hub). Los ticks y velas posteriores del MISMO periodo continúan sobre ella, con la apertura
    /// y los extremos reales en vez de los del primer dato visto.
    /// </summary>
    public void Seed(Candle forming)
    {
        var bucketStart = BucketStartUtc(forming.Time) ?? forming.Time;
        StartBucket(bucketStart, forming.Open, forming.High, forming.Low, forming.Close);
    }

    public Candle? Current => _hasBucket ? new Candle(_bucketStartUtc, _open, _high, _low, _close) : null;

    /// <summary>Inicio (UTC) del periodo al que pertenece el instante, o null si está fuera de la sesión.</summary>
    public DateTime? BucketStartUtc(DateTime utcTime)
    {
        utcTime = DateTime.SpecifyKind(utcTime, DateTimeKind.Utc);

        if (!_rthOnly)
        {
            var intervalTicks = TimeSpan.FromMinutes(_intervalMinutes).Ticks;
            return new DateTime(utcTime.Ticks - utcTime.Ticks % intervalTicks, DateTimeKind.Utc);
        }

        var eastern = TimeZoneInfo.ConvertTimeFromUtc(utcTime, Eastern);
        var timeOfDay = eastern.TimeOfDay;
        if (timeOfDay < SessionOpen || timeOfDay >= SessionClose) return null;

        var anchor = eastern.Date + SessionOpen;
        var index = (int)Math.Floor((eastern - anchor).TotalMinutes / _intervalMinutes);
        var startEastern = DateTime.SpecifyKind(anchor.AddMinutes(index * _intervalMinutes), DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(startEastern, Eastern);
    }

    private void StartBucket(DateTime startUtc, decimal open, decimal high, decimal low, decimal close)
    {
        _hasBucket = true;
        _bucketStartUtc = startUtc;
        _open = open;
        _high = high;
        _low = low;
        _close = close;
    }
}
