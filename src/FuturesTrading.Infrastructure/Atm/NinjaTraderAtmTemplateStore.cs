using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using FuturesTrading.Application.Atm;
using FuturesTrading.Domain;
using Microsoft.Extensions.Options;

namespace FuturesTrading.Infrastructure.Atm;

/// <summary>
/// Escribe plantillas ATM en la carpeta de NinjaTrader a partir de una plantilla base (ATM1030) embebida.
/// Solo cambian <c>Template</c>, <c>StopLoss</c>, <c>Target</c> y <c>AtmSelector</c> (un GUID nuevo): es lo
/// único que varía entre las plantillas que guarda NinjaTrader. Formato igual al suyo: UTF-8 con BOM,
/// CRLF y sin salto de línea final.
/// </summary>
public sealed class NinjaTraderAtmTemplateStore : IAtmTemplateStore
{
    private const string BaseResource = "AtmTemplate.base.xml";

    private static readonly Lazy<string> BaseXml = new(LoadBaseXml);

    private readonly string _folder;

    public NinjaTraderAtmTemplateStore(IOptions<AtmSettings> options)
    {
        _folder = options.Value.Folder;
    }

    public AtmTemplateResult Create(AtmTemplate template)
    {
        // No se crea la carpeta: si la unidad de red no está montada, crear "U:\" local escondería el problema.
        if (!Directory.Exists(_folder))
            throw new DirectoryNotFoundException($"No se encuentra la carpeta de plantillas ({_folder}).");

        var path = Path.Combine(_folder, template.Name + ".xml");
        if (File.Exists(path)) return AtmTemplateResult.AlreadyExists;

        var xml = Render(template);
        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        var bytes = encoding.GetPreamble().Concat(encoding.GetBytes(xml)).ToArray();

        try
        {
            // CreateNew: si otro proceso la creó entre la comprobación y la escritura, no se pisa.
            using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write);
            stream.Write(bytes);
        }
        catch (IOException) when (File.Exists(path))
        {
            return AtmTemplateResult.AlreadyExists;
        }

        return AtmTemplateResult.Created;
    }

    private static string Render(AtmTemplate template)
    {
        var xml = BaseXml.Value;
        xml = SetTag(xml, "Template", template.Name);
        xml = SetTag(xml, "StopLoss", template.StopTicks.ToString());
        xml = SetTag(xml, "Target", template.TargetTicks.ToString());
        xml = SetTag(xml, "AtmSelector", Guid.NewGuid().ToString("N"));
        return xml;
    }

    private static string SetTag(string xml, string tag, string value)
    {
        var pattern = $"<{tag}>[^<]*</{tag}>";
        if (Regex.Matches(xml, pattern).Count != 1)
            throw new InvalidOperationException($"La plantilla base debe tener exactamente un <{tag}>.");
        return Regex.Replace(xml, pattern, $"<{tag}>{value}</{tag}>");
    }

    private static string LoadBaseXml()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(BaseResource)
            ?? throw new InvalidOperationException($"Falta el recurso embebido {BaseResource}.");
        using var reader = new StreamReader(stream, Encoding.UTF8);   // descarta el BOM
        return reader.ReadToEnd();
    }
}
