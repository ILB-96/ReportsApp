using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Reports.Data;
using Reports.Services;
using Reports.Services.Crm;
using Reports.Services.Export;
using Reports.Services.Files;
using Reports.Utilities;
using MessageBox = System.Windows.MessageBox;

namespace Reports.Tabs;

public partial class CreateDriverForm : Page
{
    private readonly AppConfig _config;
    public CreateDriverForm() : this(App.Services.GetRequiredService<AppConfig>(), App.Services.GetRequiredService<ChromeTabsStore>())
    {
    }

    public CreateDriverForm(AppConfig config, ChromeTabsStore requiredService)
    {
        InitializeComponent();
        _config = config;
        DataContext = _config;
    }

    private void ClearExcelMenu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement { ContextMenu: not null } fe) return;
        fe.ContextMenu.PlacementTarget = fe;
        fe.ContextMenu.IsOpen = true;
    }

    private async void ClearExcel_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using (Loading.BeginScope("מוחק את השורות... רגע סבלנות", "זה יכול לקחת עד כמה שניות..."))
            {

                if (sender is not MenuItem mi) return;

                var serviceType = mi.Tag?.ToString()?.Trim();
                if (string.IsNullOrWhiteSpace(serviceType)) return;

                var excelPath = GetExcelPath(serviceType);

                var confirm = MessageBox.Show(
                    $"זה ימחק את כל השורות בקובץ {_config.DriversFile(serviceType)}\nלהמשיך?",
                    "אישור מחיקה",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirm != MessageBoxResult.Yes)
                    return;

                await Task.Run(() => DriversExportService.ClearRows(excelPath, _config.DriversLastColToClear));
            }

            await Overlay.ShowAsync(true, "נמחק בהצלחה.");
        }
        catch (Exception ex)
        {
            await Overlay.ShowAsync(false, ex.ToString());
        }
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
                var incidentId = crm.ExtractCrmId(Url.Text.Trim());

                var incident = await crm.GetIncidentAsync(incidentId);
                var (accountId, isLeasing, contactId, partial) = CrmParsers.ParseIncident(incident, brand);

                if (string.IsNullOrWhiteSpace(accountId))
                    throw new InvalidOperationException("Account Link is missing.");


                var account = (!isLeasing && !string.IsNullOrWhiteSpace(contactId))
                    ? await crm.GetContactAsync(contactId)
                    : await crm.GetAccountAsync(accountId);
                var data = CrmParsers.MergeAccount(partial, account);

                // if (string.IsNullOrWhiteSpace(data.Phone))
                //     throw new InvalidOperationException("Phone is missing.");

                // if (await _phones.ExistsAsync(data.Phone))
                // {
                //     await Overlay.ShowAsync(false, "This phone already exists locally (SQLite).");
                //     return;
                // }

                FillForm(data);
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
            var serviceType = ServiceTypeBox.Text;
            using (Loading.BeginScope("מייצר נהג... רגע סבלנות", "זה יכול לקחת עד כמה שניות..."))
            {
                var excelPath = "";
                var row = BuildExcelRowFromForm();
                ValidateRow(row);

                Directory.CreateDirectory(_config.DriversFolderPath);

                var accountFolder = Path.Combine(_config.DriversFolderPath, $"{row["CarLicense"]["Val"]} - {row["AccountFullName"]["Val"]}");
                Directory.CreateDirectory(accountFolder);

                await FileDownloader.DownloadIfExistsAsync(LicenseLinkBox.Text.Trim(), accountFolder, "license");
                await FileDownloader.DownloadIfExistsAsync(PassportLinkBox.Text.Trim(), accountFolder, "passport");

                ShellService.OpenDirectory(accountFolder);

                excelPath = GetExcelPath(serviceType);
                DriversExportService.AppendRow(excelPath, row);

                if (!string.IsNullOrWhiteSpace(ReservationNumberBox.Text) || brand == "autotel")
                    await GenerateAgreementAsync(brand, accountFolder, row);

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
        if (brand.Contains("Lease"))
            return "lease";
        
        if (brand.Contains("lease"))
            return "colmobil";
        
        return brand;
        
    }

    private string GetExcelPath(string serviceType)
        => Path.Combine(_config.DriversFolderPath, _config.DriversFile(serviceType));
    

    private void FillForm(DriverData d)
    {
        ServiceTypeBox.Text       = GetServiceType(d.ServiceType);
        ReportStartDateBox.Text   = d.ReportTime;
        ReportEndDateBox.Text     = d.ReportTime;

        CarLicenseBox.Text        = d.CarLicense;
        ReservationNumberBox.Text = d.ReservationNumber;
        ReportNumberBox.Text      = d.ReportNumber;

        AccountFullNameBox.Text   = d.AccountFullName;
        DriverIdBox.Text          = d.DriverId;
        DriverLicenseBox.Text     = d.DriverLicense;

        EmailBox.Text             = d.Email;
        PhoneBox.Text             = d.Phone;

        AddressBox.Text           = d.Address;
        HouseBox.Text             = d.House;
        CityBox.Text              = d.City;
        PostalCodeBox.Text        = d.PostalCode;

        CreatedOnBox.Text         = d.CreatedOn;
        LicenseLinkBox.Text       = d.LicenseLink;
        PassportLinkBox.Text      = d.PassportLink;
    }

    private Dictionary<string, Dictionary<string, object>> BuildExcelRowFromForm() => new()
    {
        ["CarLicense"]      = new() { ["Col"] = 1,  ["Val"]  = CarLicenseBox.Text.Trim().Replace("-", "") },
        ["AccountFullName"] = new() { ["Col"] = 4,  ["Val"] = AccountFullNameBox.Text.Trim() },
        ["DriverId"]        = new() { ["Col"] = 5,  ["Val"] = DriverIdBox.Text.Trim() },
        ["Phone"]           = new() { ["Col"] = 6,  ["Val"] = PhoneBox.Text.Trim() },
        ["ReportStartDate"] = new() { ["Col"] = 2,  ["Val"] = ReportStartDateBox.Text.Trim() },
        ["ReportEndDate"]   = new() { ["Col"] = 3,  ["Val"] = ReportEndDateBox.Text.Trim() },
        ["DriverLicense"]   = new() { ["Col"] = 7,  ["Val"] = DriverLicenseBox.Text.Trim() },
        ["Address"]         = new() { ["Col"] = 9,  ["Val"] = AddressBox.Text.Trim() },
        ["House"]           = new() { ["Col"] = 10,  ["Val"] = HouseBox.Text.Trim() },
        ["City"]            = new() { ["Col"] = 11, ["Val"] = CityBox.Text.Trim() },
        ["Email"]           = new() { ["Col"] = 8, ["Val"] = EmailBox.Text.Trim() },
        ["PostalCode"]      = new() { ["Col"] = 12, ["Val"] = PostalCodeBox.Text.Trim() },
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

    private void ValidateRow(Dictionary<string, Dictionary<string, object>> row)
    {
        if (row["ReportStartDate"]["Val"] == row["ReportEndDate"]["Val"])
            throw new InvalidOperationException("שנה טווח חוזה.");

        var missing = row.FirstOrDefault(kvp => string.IsNullOrWhiteSpace(kvp.Value["Val"].ToString()));
        if (string.IsNullOrEmpty(missing.Key)) return;
        
        var val = missing.Value.GetValueOrDefault("Val", "?");
        throw new InvalidOperationException($"חסרה עמודה {val}.");
    }

    private async Task GenerateAgreementAsync(string brand, string accountFolder, Dictionary<string, Dictionary<string, object>> row)
    {
        var fields = new Dictionary<string, string>
        {
            ["Name"] = row["AccountFullName"]["Val"].ToString() ?? string.Empty,
            ["Date"] = CreatedOnBox.Text.Trim()
        };

        var safeName = FileNameUtils.SanitizeFileName(row["AccountFullName"]["Val"].ToString() ?? "Default");
        var docxPath = Path.Combine(accountFolder, $"Agreement - {safeName}.docx");
        var resourceName = _config.AgreementTemplate(brand);

        await DocxTemplateGenerator.GenerateFromEmbeddedAsync(resourceName, docxPath, fields);
        await DocxTemplateGenerator.SaveToPdf(docxPath);
    }
}
