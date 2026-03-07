using System.IO;
using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Reports.Configuration;
using Reports.Utilities;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Microsoft.Extensions.Logging;
using Reports.Services;
using Reports.Tabs;

namespace Reports;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IHost? _host;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        UserSettings.Load();
        
        ApplicationThemeManager.Apply(
            ApplicationTheme.Light,   // or Dark
            WindowBackdropType.Mica,  // or Tabbed, Acrylic, etc.
            true                      // force update existing windows
        );
        
        _host = Host.CreateDefaultBuilder()
            .ConfigureLogging(logging =>
            {
                // Get rid of providers that can trigger EventLog
                logging.ClearProviders();

                // Desktop-friendly providers
                logging.AddDebug();
                // logging.AddConsole(); // optional (WPF usually doesn't need it)
            })
            .ConfigureAppConfiguration((ctx, cfg) =>
            {
                cfg.Sources.Clear();
                cfg.SetBasePath(AppContext.BaseDirectory)
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddEnvironmentVariables();
            })
            .ConfigureServices((ctx, services) =>
            {
                services.Configure<AppOptions>(ctx.Configuration.GetSection("App"));
                services.AddSingleton<AppConfig>();
                services.AddSingleton<CreateDriverForm>();
                services.AddSingleton<Tabs.SignatureForm>();
                services.AddSingleton<Tabs.CreateCustomerRequest>();
                services.AddSingleton<Tabs.CreateOperationMail>();
                services.AddSingleton<Tabs.AgreementForm>();
                services.AddSingleton<Tabs.ReservationForm>();
                services.AddSingleton<Tabs.ShortcutsPage>();
                services.AddSingleton<Tabs.CreateIncidentForm>();
                services.AddSingleton<ChromeTabsStore>();
                services.AddHostedService<ChromeTabsListener>();
                services.AddSingleton<MainWindow>();
            })
            .Build();

        _host.Start();
        Services = _host.Services;
        
        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnExit(ExitEventArgs e)
    {
        _host?.Dispose();
        base.OnExit(e);
    }
}