using System.Windows;
using Reports.Configuration;
using Reports.Tabs.CreateDriver;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Reports.Configuration;
using Reports.Utilities;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;
using Microsoft.Extensions.Logging;
using Reports.Services.Agreement;
using Reports.Services.BetterwayApi;
using Reports.Services.ChromeSync;
using Reports.Services.Crm;
using Reports.Services.Drivers;
using Reports.Services.Email;
using Reports.Services.Email.CustomerRequests;
using Reports.Services.Email.OperationMail;
using Reports.Services.Files;
using Reports.Services.GotoTech;
using Reports.Services.Incident;
using Reports.Services.Navigation;
using Reports.Services.Templates;
using Reports.Tabs;
using Reports.Tabs.CreateAgreement;
using Reports.Tabs.CreateDriver;
using Reports.Tabs.CreateIncident;
using Reports.Tabs.CreateReservation;
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
                services.AddSingleton<CreateReservationPage>();
                services.AddSingleton<CreateIncident>();
                services.AddSingleton<IDriverDraftService, DriverDraftService>();
                services.AddSingleton<IIncidentDraftService, IncidentDraftService>();
                services.AddSingleton<IIncidentSubmissionService, IncidentSubmissionService>();
                services.AddSingleton<IAgreementSubmissionService, AgreementSubmissionService>();
                services.AddSingleton<IAgreementDraftService, AgreementDraftService>();
                
                services.AddSingleton<IBetterwayVehicleSearch, BetterwayVehicleSearch>();
                services.AddSingleton<IDriverSubmissionService, DriverSubmissionService>();
                services.AddSingleton<ICrmBrandResolver, CrmBrandResolver>();
                services.AddSingleton<IDriverPaths, DriverPaths>();
                services.AddSingleton<ITemplateCatalog, TemplateCatalog>();
                services.AddSingleton<IWordPdfExporter, WordPdfExporter>();
                services.AddSingleton<IFileDownloader, FileDownloaderService>();
                services.AddSingleton<IShellService, ShellServiceAdapter>();
                services.AddSingleton<IDocxTemplateGenerator, DocxTemplateGeneratorAdapter>();
                services.AddSingleton<IAddressParser, AddressParser>();
                services.AddSingleton<IEmailComposerService, EmailComposerService>();

                services.AddTransient<IEmailDraftBuilder<CustomerRequestEmailModel>, CustomerRequestEmailDraftBuilder>();
                services.AddTransient<IEmailDraftBuilder<OperationMailModel>, OperationMailDraftBuilder>();
                services.AddSingleton<SignatureForm>();
                services.AddSingleton<CreateCustomerRequest>();
                services.AddSingleton<CreateOperationMail>();
                services.AddSingleton<CreateAgreement>();
                services.AddSingleton<ReservationForm>();
                services.AddSingleton<ShortcutsPage>();
                services.AddSingleton<ChromeSyncStore>();
                services.AddHostedService<ChromeTabsListener>();
                services.AddSingleton<ICrmCookieProvider, CrmCookieProvider>();
                services.AddSingleton<MainWindow>();
                services.AddSingleton<IGotoTechTokenProvider, GotoTechTokenProvider>();
                services.AddHttpClient<GotoTechApiClient>();
                services.AddSingleton<IAgreementGenerator, AgreementGenerator>();

                
                services.AddHttpClient<IBetterwayTokenProvider, BetterwayTokenProvider>(c =>
                {
                    c.BaseAddress = new Uri("https://api.betterway.co.il/");
                });

                services.AddHttpClient<IBetterwayDriverApi, BetterwayNewDriver>(c =>
                {
                    c.BaseAddress = new Uri("https://api.betterway.co.il/");
                    c.DefaultRequestHeaders.Add("Origin", "https://app.betterway.co.il");
                    c.DefaultRequestHeaders.Add("Referer", "https://app.betterway.co.il/");
                });
                services.AddHttpClient<IBetterwayDriverSearch, BetterwayDriverSearch>(c =>
                {
                    c.BaseAddress = new Uri("https://api.betterway.co.il/");
                    c.DefaultRequestHeaders.Add("Origin", "https://app.betterway.co.il");
                    c.DefaultRequestHeaders.Add("Referer", "https://app.betterway.co.il/");
                });
                services.AddHttpClient<IBetterwayClientSearch, BetterwayClientSearch>(c =>
                {
                    c.BaseAddress = new Uri("https://api.betterway.co.il/");
                    c.DefaultRequestHeaders.Add("Origin", "https://app.betterway.co.il");
                    c.DefaultRequestHeaders.Add("Referer", "https://app.betterway.co.il/");
                });
                services.AddHttpClient<IBetterwayVehicleSearch, BetterwayVehicleSearch>(c =>
                {
                    c.BaseAddress = new Uri("https://api.betterway.co.il/");
                    c.DefaultRequestHeaders.Add("Origin", "https://app.betterway.co.il");
                    c.DefaultRequestHeaders.Add("Referer", "https://app.betterway.co.il/");
                });
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