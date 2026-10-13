using FuturesTrading.Application.Auth;
using FuturesTrading.Infrastructure;
using FuturesTrading.Infrastructure.Auth;
using FuturesTrading.UI.Forms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WinFormsApplication = System.Windows.Forms.Application;

namespace FuturesTrading.UI;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        var services = new ServiceCollection();

        services.Configure<ApiSettings>(configuration.GetSection("Api"));
        services.AddHttpClient<IAuthService, BackendApiClient>();
        services.AddTransient<HomeForm>();
        services.AddTransient<LoginForm>(); // en pausa (spec 0002): registrado, pero no se muestra al arrancar

        using var serviceProvider = services.BuildServiceProvider();

        WinFormsApplication.Run(serviceProvider.GetRequiredService<HomeForm>());
    }
}
