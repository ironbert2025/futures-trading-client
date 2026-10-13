namespace FuturesTrading.E2E.Tests;

/// <summary>Razones de tests omitidos. Un Skip sin razón escrita no se acepta en este repo.</summary>
internal static class PausedReasons
{
    public const string Login =
        "Login en pausa (spec 0002): la app arranca en HomeForm. Quitar el Skip al retomar la spec 0001 (tarea T6 de 0002).";
}
