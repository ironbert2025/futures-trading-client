using FuturesTrading.Domain;

namespace FuturesTrading.Application.Atm;

public enum AtmTemplateResult
{
    Created,
    AlreadyExists,
}

/// <summary>
/// Guarda plantillas ATM donde NinjaTrader las lee. Nunca pisa una plantilla existente.
/// </summary>
public interface IAtmTemplateStore
{
    /// <exception cref="IOException">No se pudo escribir (carpeta inaccesible, red caída, etc.).</exception>
    /// <exception cref="UnauthorizedAccessException">Sin permiso de escritura en la carpeta.</exception>
    AtmTemplateResult Create(AtmTemplate template);
}
