using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using Reports.Services;
using Reports.Services.ChromeSync;
using Reports.Services.Crm;
using Reports.Services.Email;
using Reports.Services.Email.OperationMail;
using Reports.Utilities;

namespace Reports.Tabs;

public partial class CreateOperationMail
{
    private readonly ICrmBrandResolver _brandResolver;
    private readonly IEmailDraftBuilder<OperationMailModel> _draftBuilder;
    private readonly IEmailComposerService _emailComposer;
    private readonly ICrmCookieProvider _cookieProvider;

    public ChromeSyncStore SyncStore { get; }

    public CreateOperationMail(
        ChromeSyncStore syncStore,
        ICrmBrandResolver brandResolver,
        IEmailDraftBuilder<OperationMailModel> draftBuilder,
        IEmailComposerService emailComposer,
        ICrmCookieProvider cookieProvider)
    {
        InitializeComponent();
        SyncStore = syncStore;
        _brandResolver = brandResolver;
        _draftBuilder = draftBuilder;
        _emailComposer = emailComposer;
        _cookieProvider = cookieProvider;
        DataContext = this;
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
                var brand = _brandResolver.ServiceTypeFromUrl(Url.Text);
                var baseUri = _brandResolver.BaseUri(brand);

                var cookies = _cookieProvider.GetCookiesForUrl(Url.Text.Trim());
        
                if (cookies.Count == 0)
                    throw new InvalidOperationException("No CRM cookies were found for this URL. Open the CRM tab in Chrome and try again.");

                using var crm = new CrmApi(CrmClientFactory.Create(baseUri, cookies));
                var incidentId = crm.ExtractCrmId(Url.Text.Trim());

                var incident = await crm.GetIncidentAsync(incidentId);
                var (_, _, _, partial) = CrmParsers.ParseIncident(incident, brand);

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
        AccountFullNameBox.Text = d.DriverName
            .Replace("אוטותל", "")
            .Replace("גוטו", "")
            .Replace("תפעול", "")
            .Trim();
    }

    private async void SendFinal_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using (Loading.BeginScope("מייצר מייל... רגע סבלנות", "זה יכול לקחת עד כמה שניות..."))
            {
                var brand = _brandResolver.ServiceTypeFromUrl(Url.Text);

                var model = new OperationMailModel(
                    Brand: brand,
                    AccountFullName: AccountFullNameBox.Text.Trim(),
                    ReportNumber: ReportNumberBox.Text.Trim(),
                    ReportReason: ReportReasonBox.Text.Trim(),
                    CarLicense: CarLicenseBox.Text.Trim(),
                    ReportDate: ReportDateBox.Text.Trim(),
                    ReportAddress: ReportAddressBox.Text.Trim(),
                    ReportPrice: ReportPriceBox.Text.Trim(),
                    ReportCity: ReportCityBox.Text.Trim());

                var draft = _draftBuilder.Build(model);
                _emailComposer.OpenDraft(draft);

                ShowFirstPage();
            }
        }
        catch (Exception ex)
        {
            await Overlay.ShowAsync(false, ex.Message);
        }
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