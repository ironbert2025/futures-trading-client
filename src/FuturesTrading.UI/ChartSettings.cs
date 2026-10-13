namespace FuturesTrading.UI;

/// <summary>Configuración de un gráfico. Un símbolo vacío deja el panel reservado pero inactivo.</summary>
public sealed class ChartSettings
{
    /// <summary>Símbolo que se grafica, tal como lo emite el hub (p. ej. "/MESZ26"). El hub emite varios símbolos; aquí se filtra uno.</summary>
    public string Symbol { get; init; } = "";

    /// <summary>
    /// true: solo sesión regular de acciones (9:30–16:00 ET), p. ej. SPY o TSLA.
    /// false: 24 horas, p. ej. el futuro /MESZ26.
    /// </summary>
    public bool RthOnly { get; init; } = true;

    /// <summary>
    /// Mínima variación de precio del contrato (p. ej. 0.25 para /MES). El precio se mueve de tick en
    /// tick: el eje usa esta granularidad y las líneas DZ/SZ se ajustan a ella. 0 = sin dato (sin ajuste).
    /// </summary>
    public decimal TickSize { get; init; }
}

/// <summary>Los gráficos de la pestaña Charts, en orden: arriba-izquierda, arriba-derecha, abajo-izquierda, abajo-derecha.</summary>
public sealed class ChartsSettings
{
    public List<ChartSettings> Panels { get; init; } = new();
}
