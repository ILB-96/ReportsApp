using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Reports.Services;
using Reports.Services.Crm;
using Reports.Utilities;

namespace Reports.Tabs;

public partial class CreateIncidentForm : Page
{
    private readonly AppConfig _config;

    public CreateIncidentForm() : this(App.Services.GetRequiredService<AppConfig>())
    {
    }

    public CreateIncidentForm(AppConfig config)
    {
        InitializeComponent();
        _config = config;
        
        DataContext = App.Services.GetRequiredService<AppConfig>();
    }

    private async void Submit_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using (Loading.BeginScope("מיצא את פרטי הנהג... רגע סבלנות", "זה יכול לקחת עד כמה שניות..."))
            {
                var brand = _config.ServiceTypeFromUrl(Url.Text);
                var baseUri = _config.BaseUri(brand);

                var cookiesRaw = Cookies.Text.Trim();
                UserSettings.Save(cookiesRaw);
                var cookies = CookieExtractor.ExtractCrmOwinCookies(UserSettings.LastCookie);

                using var crm = new CrmApi(CrmClientFactory.Create(baseUri, cookies));
                var accountId = crm.ExtractCrmId(Url.Text.Trim());

                // var incident = await crm.GetIncidentAsync(accountId);
                // var (accountId, isLeasing, contactId, partial) = CrmParsers.ParseIncident(incident, brand);

                if (string.IsNullOrWhiteSpace(accountId))
                    throw new InvalidOperationException("Account Link is missing.");


                // var account = (!isLeasing && !string.IsNullOrWhiteSpace(contactId))
                //     ? await crm.GetContactAsync(contactId)
                //     : await crm.GetAccountAsync(accountId);
                // var data = CrmParsers.MergeAccount(partial, account);

                // if (string.IsNullOrWhiteSpace(data.Phone))
                //     throw new InvalidOperationException("Phone is missing.");

                // if (await _phones.ExistsAsync(data.Phone))
                // {
                //     await Overlay.ShowAsync(false, "This phone already exists locally (SQLite).");
                //     return;
                // }

                // FillForm(data);
                ShowSecondPage();
            }
        }
        catch (Exception ex)
        {
            await Overlay.ShowAsync(false, ex.ToString());
        }
    }

    private async void SendFinal_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var brand = _config.ServiceTypeFromUrl(Url.Text);
            var serviceType = GetServiceType(brand);
            using (Loading.BeginScope("מייצר נהג... רגע סבלנות", "זה יכול לקחת עד כמה שניות..."))
            {

                // await _phones.InsertAsync(row[4]);

                ShowFirstPage();
            }

            await Overlay.ShowAsync(true, $"שורה נוספה לקובץ {_config.DriversFile(serviceType)}", 4000);
        }
        catch (Exception ex)
        {
            await Overlay.ShowAsync(false, ex.ToString());
        }
    }
    
    private string GetServiceType(string brand)
    {
        var isLeasing = bool.TryParse(IsLeasing.Text.Trim(), out var b) && b;
        return isLeasing ? "leasing" : brand;
    }


    private void PreviousPage_Click(object sender, RoutedEventArgs e) => ShowFirstPage();

    private void ShowFirstPage()
    {
        InputPanel.Visibility = Visibility.Visible;
        DataPanel.Visibility = Visibility.Collapsed;
    }

    private void ShowSecondPage()
    {
        InputPanel.Visibility = Visibility.Collapsed;
        DataPanel.Visibility = Visibility.Visible;
    }
    
}
