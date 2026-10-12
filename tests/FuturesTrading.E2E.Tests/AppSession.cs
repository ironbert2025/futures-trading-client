using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FlaUI.Core.Tools;
using FlaUI.UIA3;

namespace FuturesTrading.E2E.Tests;

/// <summary>
/// Lanza la app WinForms real (ya compilada) y expone helpers de UI Automation.
/// Cada test obtiene su propia instancia y se cierra al hacer Dispose.
/// </summary>
public sealed class AppSession : IDisposable
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(10);

    private readonly Application _app;
    private readonly UIA3Automation _automation = new();

    public Window LoginWindow { get; }

    /// <param name="apiBaseUrl">
    /// Si se indica, la app arranca con Api__BaseUrl apuntando ahí (backend falso). Si no, usa el
    /// appsettings.json (backend real en localhost:8000).
    /// </param>
    public AppSession(string? apiBaseUrl = null)
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo(FindExecutablePath())
        {
            UseShellExecute = false,
        };

        if (apiBaseUrl is not null)
        {
            startInfo.Environment["Api__BaseUrl"] = apiBaseUrl;
        }

        _app = Application.Launch(startInfo);
        LoginWindow = _app.GetMainWindow(_automation, DefaultTimeout)
            ?? throw new InvalidOperationException("No apareció la ventana de login.");
    }

    public bool IsEnabled(string automationId)
    {
        var element = LoginWindow.FindFirstDescendant(cf => cf.ByAutomationId(automationId))
            ?? throw new InvalidOperationException($"No se encontró el control '{automationId}'.");
        return element.IsEnabled;
    }

    /// <summary>Espera a que el proceso de la app termine por sí solo.</summary>
    public bool WaitForExit(TimeSpan timeout) =>
        Retry.WhileFalse(() => _app.HasExited, timeout, TimeSpan.FromMilliseconds(200)).Success;

    public void TypeInto(string automationId, string text)
    {
        var field = LoginWindow.FindFirstDescendant(cf => cf.ByAutomationId(automationId))
            ?? throw new InvalidOperationException($"No se encontró el control '{automationId}'.");
        field.Focus();
        Keyboard.Type(text);
    }

    public void Click(string automationId)
    {
        var button = LoginWindow.FindFirstDescendant(cf => cf.ByAutomationId(automationId))
            ?? throw new InvalidOperationException($"No se encontró el control '{automationId}'.");
        button.AsButton().Invoke();
    }

    /// <summary>Espera a que un control (por AutomationId) exista y sea visible en la ventana dada.</summary>
    public AutomationElement? WaitForElement(Window window, string automationId)
    {
        return Retry.WhileNull(
            () => window.FindFirstDescendant(cf => cf.ByAutomationId(automationId)),
            DefaultTimeout,
            TimeSpan.FromMilliseconds(200)).Result;
    }

    /// <summary>Espera a que aparezca una ventana de nivel superior con el título dado.</summary>
    public Window? WaitForWindow(string title)
    {
        return Retry.WhileNull(
            () => _app.GetAllTopLevelWindows(_automation).FirstOrDefault(w => w.Title == title),
            DefaultTimeout,
            TimeSpan.FromMilliseconds(200)).Result;
    }

    /// <summary>Búsqueda inmediata (sin espera) de una ventana por título; null si no existe ahora.</summary>
    public Window? FindWindowNow(string title)
    {
        return _app.GetAllTopLevelWindows(_automation).FirstOrDefault(w => w.Title == title);
    }

    public void Dispose()
    {
        try
        {
            if (!_app.HasExited)
            {
                _app.Kill();
            }
        }
        finally
        {
            _automation.Dispose();
        }
    }

    private static string FindExecutablePath()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "FuturesTrading.slnx")))
        {
            dir = dir.Parent;
        }

        if (dir is null)
        {
            throw new InvalidOperationException("No se encontró la raíz de la solución (FuturesTrading.slnx).");
        }

        var exe = Path.Combine(dir.FullName, "src", "FuturesTrading.UI", "bin", "Debug", "net8.0-windows", "FuturesTrading.UI.exe");
        if (!File.Exists(exe))
        {
            throw new FileNotFoundException("La UI no está compilada (Debug).", exe);
        }

        return exe;
    }
}
