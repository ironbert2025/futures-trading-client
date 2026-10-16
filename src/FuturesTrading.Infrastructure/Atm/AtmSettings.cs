namespace FuturesTrading.Infrastructure.Atm;

public sealed class AtmSettings
{
    /// <summary>
    /// Carpeta donde NinjaTrader lee las plantillas ATM (Documents\NinjaTrader 8\templates\AtmStrategy,
    /// aquí vista como la unidad de red U:). Puede ser una ruta UNC para no depender de la letra.
    /// </summary>
    public string Folder { get; init; } = @"U:\";
}
