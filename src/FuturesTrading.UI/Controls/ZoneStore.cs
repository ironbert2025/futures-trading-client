using System.Text.Json;

namespace FuturesTrading.UI.Controls;

/// <summary>Punto de una línea de zona: tiempo (hora de pared de NY "disfrazada de UTC", segundos) y precio.</summary>
public sealed record ZonePoint(long Time, double Price);

/// <summary>Zona DZ/SZ completa: línea verde (Demanda) y línea roja (Supply).</summary>
public sealed record ZoneDto(ZonePoint Demand, ZonePoint Supply);

/// <summary>
/// Guarda las zonas DZ/SZ dibujadas, por símbolo, en un JSON local
/// (%LocalAppData%\FuturesTrading\zones.json) para mostrarlas en la próxima ejecución.
/// Un archivo ilegible se trata como "sin zonas": nunca debe impedir que arranque la app.
/// </summary>
public static class ZoneStore
{
    private static readonly object Gate = new();
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FuturesTrading", "zones.json");

    public static IReadOnlyList<ZoneDto> Load(string symbol)
    {
        lock (Gate)
        {
            var all = ReadAll();
            return all.TryGetValue(symbol, out var zones) ? zones : [];
        }
    }

    public static void Save(string symbol, IReadOnlyList<ZoneDto> zones)
    {
        lock (Gate)
        {
            var all = ReadAll();
            if (zones.Count == 0) all.Remove(symbol);
            else all[symbol] = zones.ToList();

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
                // Escritura atómica: si la app muere a medias no queda un JSON a medio escribir.
                var temp = FilePath + ".tmp";
                File.WriteAllText(temp, JsonSerializer.Serialize(all, Json));
                File.Move(temp, FilePath, overwrite: true);
            }
            catch (IOException) { /* no poder guardar no debe tumbar el gráfico */ }
            catch (UnauthorizedAccessException) { }
        }
    }

    private static Dictionary<string, List<ZoneDto>> ReadAll()
    {
        try
        {
            if (!File.Exists(FilePath)) return new(StringComparer.OrdinalIgnoreCase);
            var parsed = JsonSerializer.Deserialize<Dictionary<string, List<ZoneDto>>>(File.ReadAllText(FilePath), Json);
            return new(parsed ?? new(), StringComparer.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return new(StringComparer.OrdinalIgnoreCase);
        }
    }
}
