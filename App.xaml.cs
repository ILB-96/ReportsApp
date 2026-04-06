using System;
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
using Reports.Data;
using Reports.Services;
using Reports.Services.Crm;
using Reports.Services.Drivers;
using Reports.Services.Email;
using Reports.Services.Email.CustomerRequests;
using Reports.Services.Email.OperationMail;
using Reports.Services.Export;
using Reports.Services.Files;
using Reports.Services.Navigation;
using Reports.Services.Templates;
using Reports.Tabs;
using Reports.Tabs.CreateDriver;
using Wpf.Ui.Abstractions;

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
                services.AddSingleton<INavigationViewPageProvider, DependencyInjectionPageProvider>();
                services.AddSingleton<CreateDriverPage>();
                services.AddSingleton<IDriverDraftService, DriverDraftService>();
                services.AddSingleton<IDriverSubmissionService, DriverSubmissionService>();
                services.AddSingleton<ICrmBrandResolver, CrmBrandResolver>();
                services.AddSingleton<IDriverPaths, DriverPaths>();
                services.AddSingleton<ITemplateCatalog, TemplateCatalog>();
                services.AddSingleton<IWordPdfExporter, WordPdfExporter>();
                services.AddSingleton<IFileDownloader, FileDownloaderService>();
                services.AddSingleton<IShellService, ShellServiceAdapter>();
                services.AddSingleton<IDriversExportService, DriversExportServiceAdapter>();
                services.AddSingleton<IDocxTemplateGenerator, DocxTemplateGeneratorAdapter>();
                services.AddSingleton<IAddressParser, AddressParser>();
                services.AddSingleton<IEmailComposerService, EmailComposerService>();

                services.AddTransient<IEmailDraftBuilder<CustomerRequestEmailModel>, CustomerRequestEmailDraftBuilder>();
                services.AddTransient<IEmailDraftBuilder<OperationMailModel>, OperationMailDraftBuilder>();
                services.AddSingleton<SignatureForm>();
                services.AddSingleton<CreateCustomerRequest>();
                services.AddSingleton<CreateOperationMail>();
                services.AddSingleton<AgreementForm>();
                services.AddSingleton<ReservationForm>();
                services.AddSingleton<ShortcutsPage>();
                services.AddSingleton<CreateIncidentForm>();
                services.AddSingleton<ChromeTabsStore>();
                services.AddHostedService<ChromeTabsListener>();
                services.AddSingleton<MainWindow>();
                
                var dbPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Reports",
                    "reports.db");

                services.AddSingleton(new AppDb(dbPath));
                services.AddSingleton<PhonesRepository>();
            })
            .Build();

        _host.Start();
        Services = _host.Services;
        var phonesRepository = _host.Services.GetRequiredService<PhonesRepository>();
        phonesRepository.EnsureCreatedAsync().GetAwaiter().GetResult();
        
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