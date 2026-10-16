namespace FuturesTrading.Domain;

/// <summary>
/// Plantilla ATM de NinjaTrader: un contrato, un solo bracket con stop y profit medidos en ticks.
/// El nombre es ATM + stop + profit, cada número con mínimo 2 dígitos (cero a la izquierda):
/// stop 1 / profit 3 es ATM0103, stop 62 / profit 124 es ATM62124. El profit nunca es menor que el stop.
/// </summary>
public sealed record AtmTemplate
{
    public AtmTemplate(int stopTicks, int targetTicks)
    {
        if (stopTicks < 1)
            throw new ArgumentOutOfRangeException(nameof(stopTicks), stopTicks, "El stop debe ser de al menos 1 tick.");
        if (targetTicks < stopTicks)
            throw new ArgumentOutOfRangeException(nameof(targetTicks), targetTicks, $"El profit ({targetTicks}) no puede ser menor que el stop ({stopTicks}).");

        StopTicks = stopTicks;
        TargetTicks = targetTicks;
    }

    public int StopTicks { get; }

    public int TargetTicks { get; }

    public string Name => $"ATM{StopTicks:D2}{TargetTicks:D2}";
}
