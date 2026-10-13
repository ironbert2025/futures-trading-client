namespace FuturesTrading.Infrastructure.Hub;

public sealed class HubSettings
{
    /// <summary>IP o nombre del equipo donde corre OptionsTrader como hub (127.0.0.1 si es esta misma PC).</summary>
    public string Host { get; init; } = "127.0.0.1";

    public int Port { get; init; } = 51919;
}
