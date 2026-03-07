using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using Reports.Services;
using Reports.Services.Crm;
using Reports.Utilities;

namespace Reports.Tabs;

public partial class CreateOperationMail
{
    private readonly AppConfig _config;
    private readonly ChromeTabsStore _store;
    public CreateOperationMail() : this(App.Services.GetRequiredService<AppConfig>(),App.Services.GetRequiredService<ChromeTabsStore>())
    {
    }
    public CreateOperationMail(AppConfig config, ChromeTabsStore store)
    {
        InitializeComponent();
        _config = config;
        _store = store;
        DataContext = _store;
    }
    private void NumberOnly(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !MyRegex().IsMatch(e.Text);
    }
    [GeneratedRegex("^[0-9-]+$")]
    private static partial Regex MyRegex();

    
    private async void Submit_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using (Loading.BeginScope("מיצא את פרטי הדוח... רגע סבלנות", "זה יכול לקחת עד כמה שניות..."))
            {
                var brand = _config.ServiceTypeFromUrl(Url.Text);
                var baseUri = _config.BaseUri(brand);

                var cookiesRaw = Cookies.Text.Trim();
                UserSettings.Save(cookiesRaw);
                var cookies = CookieExtractor.ExtractCrmOwinCookies(UserSettings.LastCookie);

                using var crm = new CrmApi(CrmClientFactory.Create(baseUri, cookies));
                var incidentId = crm.ExtractCrmId(Url.Text.Trim());

                var incident = await crm.GetIncidentAsync(incidentId);
                var (accountId, isLeasing, contactId, partial) = CrmParsers.ParseIncident(incident, brand);
                FillForm(partial);
                ShowSecondPage();
            }
        }
        catch (Exception ex)
        {
            await Overlay.ShowAsync(false, ex.Message);
        }
    }

    private void FillForm(DriverData d)
    {
        ReportDateBox.Text = d.ReportTime;
        ReportPriceBox.Text = d.ReportPrice;
        ReportAddressBox.Text = d.ReportAddress;
        ReportCityBox.Text = d.ReportCity;
        CarLicenseBox.Text = d.CarLicense;
        ReportNumberBox.Text = d.ReportNumber;
        ReportReasonBox.Text = d.ReportReason;
        AccountFullNameBox.Text = d.DriverName.Replace("אוטותל", "").Replace("גוטו", "").Replace("תפעול", "").Trim();
    }

    private async void SendFinal_Click(object sender, RoutedEventArgs e)
    {
        try
        {

            using (Loading.BeginScope("מייצר מייל... רגע סבלנות", "זה יכול לקחת עד כמה שניות..."))
            {
                var brand = _config.ServiceTypeFromUrl(Url.Text);
                var data = BuildData();
                var outlookType = Type.GetTypeFromProgID("Outlook.Application");
                if (outlookType == null)
                {
                    await Overlay.ShowAsync(false, "Outlook is not installed on this computer.");
                    return;
                }

                dynamic? outlookApp = Activator.CreateInstance(outlookType);
                var mail = outlookApp!.CreateItem(0); // 0 = olMailItem
                var managerMail = brand == "goto" ? "binyamin.reuven@gotoglobal.com" : "idan.gur@gotoglobal.com";
                
                mail.To = $"{managerMail};";
                mail.CC = "yifat.vio@gotoglobal.com;";
                mail.Subject = $"בקשה להסבת דוח תפעול - {data["AccountFullName"]} - {data["ReportNumber"]}";

                mail.Display();
                var customBody = $@"
                        <html>
                          <body style='font-family: Arial, sans-serif; font-size: 14.5px; direction: rtl; text-align: right; line-height: 1.6;'>
                            <p style='margin: 0 0 12px 0;'>היי,</p>
                            
                            התקבל דוח בגין {data["ReportReason"]}
                            <br><br>
                            <u>כל המידע אודות הדוח מופיע כאן למטה:</u>
                            <br>
                            מספר דוח: {data["ReportNumber"]}
                             <br>
                            מספר הרכב: {data["CarLicense"]}
                             <br>
                            תאריך ושעה: {data["ReportDate"]}
                             <br>
                            כתובת: {data["ReportAddress"]} 
                             <br>
                            סכום הדוח: {data["ReportPrice"]}
                             <br>
                            רשות: {data["ReportCity"]}
                             <br><br>
                                   לפי הנתונים נראה שהדוח שייך ל{data["AccountFullName"]}.
                          </body>
                        </html>";


                mail.HTMLBody = customBody + mail.HTMLBody;
                
                ShowFirstPage();
            }
        }
        catch(Exception ex)
        {
            await Overlay.ShowAsync(false, ex.Message);
        }
        
    }
    private Dictionary<string, string> BuildData() => new()
    {
        ["CarLicense"]      = CarLicenseBox.Text.Trim(),
        ["AccountFullName"] = AccountFullNameBox.Text.Trim(),
        ["ReportNumber"]    = ReportNumberBox.Text.Trim(),
        ["ReportPrice"]     = ReportPriceBox.Text.Trim(),
        ["ReportDate"]      = ReportDateBox.Text.Trim(),
        ["ReportAddress"]   = ReportAddressBox.Text.Trim(),
        ["ReportCity"]      = ReportCityBox.Text.Trim(),
        ["ReportReason"]     = ReportReasonBox.Text.Trim(),
    };
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