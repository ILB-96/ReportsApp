using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using DocumentFormat.OpenXml.Drawing.Charts;
using Reports.Services.BetterwayApi;
using Reports.Services.ChromeSync;
using Reports.Services.Crm;
using Reports.Services.Drivers;
using Reports.Services.GotoTech;
using Wpf.Ui.Controls;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using Thickness = System.Windows.Thickness;

namespace Reports.Tabs.CreateDriver;

public partial class CreateDriverPage : Page
{
    private readonly IDriverDraftService _driverDraftService;
    private readonly IDriverSubmissionService _driverSubmissionService;
    private readonly ICrmBrandResolver _brandResolver;
    private readonly IBetterwayDriverSearch _betterwayDriverSearch;
    private readonly IBetterwayClientSearch _betterwayClientSearch;
    public ChromeSyncStore SyncStore { get; }
    public IReadOnlyList<string> ServiceTypes { get; }

    public CreateDriverView View { get; }
    public CreateDriverPage(
        ChromeSyncStore syncStore,
        ICrmBrandResolver brandResolver,
        IDriverDraftService driverDraftService,
        IDriverSubmissionService driverSubmissionService,
        IBetterwayDriverSearch betterwayDriverSearch,
        IBetterwayClientSearch betterwayClientSearch)
    {
        InitializeComponent();

        _brandResolver = brandResolver;
        _driverDraftService = driverDraftService;
        _driverSubmissionService = driverSubmissionService;
        _betterwayDriverSearch = betterwayDriverSearch;
        _betterwayClientSearch = betterwayClientSearch;

        ServiceTypes = _brandResolver.ServiceTypes;
        SyncStore = syncStore;
        View = new CreateDriverView();
        
        DataContext = this;
    }
    

    private async void Submit_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using var scope = Loading.BeginScope(
                "מייצא את פרטי הנהג... רגע סבלנות",
                "זה יכול לקחת עד כמה שניות...",
                cancelable: true);
            var ct = scope.Token;
            var (draft, reservation) = await _driverDraftService.LoadDraftAsync(View.ToDraftRequest(), ct);
            View.FillFromReservation(reservation);
            View.FillFromDraft(draft);

            var driversResult = await _betterwayDriverSearch.SearchAllProfilesAsync(draft.Phone, ct);


            DriverSearchHit? chosen = null;
            switch (driversResult.AllHits.Count)
            {
                case 1:
                {
                    chosen = driversResult.AllHits[0];
                    var profilesList = string.Join(", ", driversResult.ProfilesWithMatch);
                    MessageBox.Show(
                        $"Driver found in: {profilesList}",
                        "Driver found!",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    break;
                }
                case > 1:
                {
                    var picked = await PickDriverAsync(driversResult.AllHits);
                    if (picked is null) return; 
                    chosen = picked;
                    break;
                }
            }

            if (chosen is not null)
            {
                View.FillFromDriver(chosen.Driver);
            }

            var clientsResult = await _betterwayClientSearch.SearchAllProfilesAsync(View.DriverId, ct);

            if (clientsResult.AllHits.Count >= 1)
            {
                var profilesList = string.Join(", ", clientsResult.ProfilesWithMatch); // was driversResult
                MessageBox.Show(
                    $"Client found in: {profilesList}",
                    "Client found!",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }

            ct.ThrowIfCancellationRequested();
            View.ShowData();
        }
        catch (OperationCanceledException ex)
        {
            await Overlay.ShowAsync(false, "ביטול ידני על ידי המשתמש.");
        }
        catch (Exception ex)
        {
            await Overlay.ShowAsync(false, ex.ToString());
        }
    }

    private async void SendData_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using (Loading.BeginScope("מייצר קבצים... רגע סבלנות", "זה יכול לקחת עד כמה שניות..."))
            {
                var brand = _brandResolver.ServiceTypeFromUrl(View.Url);
                var submission = View.ToSubmission(brand);
                var reservation = View.ToReservation(brand);

                var result = await _driverSubmissionService.SubmitAsync(submission, reservation);

                var success = !result.ResponseBody.Contains("לא נמצא") && !result.ResponseBody.Contains("קיים");
                if (success)
                {
                    View.ShowInput();
                    View.ClearFields();
                }

                await Overlay.ShowAsync(success, $"Create driver response {result.ResponseBody}", 4000);
            }
        }
        catch (Exception ex)
        {
            await Overlay.ShowAsync(false, ex.ToString());
        }
    }
    

    private void ToInputPanel_Click(object sender, RoutedEventArgs e) => View.ShowInput();
    
    private void ToDataPanel_Click(object sender, RoutedEventArgs e) => View.ShowData();
    
    private void ToReservationPanel_Click(object sender, RoutedEventArgs e) => View.ShowReservation();
    
    private async Task<DriverSearchHit?> PickDriverAsync(IReadOnlyList<DriverSearchHit> hits)
    {
        var host = ((MainWindow)Application.Current.MainWindow).RootContentDialogPresenter;

        var listBox = new ListBox
        {
            ItemsSource = hits.Select(h => $"{h.Profile} — {h.Driver.Name} (ת.ז. {h.Driver.IdNumber})").ToList(),
            Margin = new Thickness(0, 10, 0, 0)
        };

        var dialog = new ContentDialog(host)
        {
            Title = "נמצאו מספר נהגים — בחר אחד",
            Content = listBox,
            PrimaryButtonText = "בחר",
            CloseButtonText = "ביטול",
            FlowDirection = FlowDirection.RightToLeft,
            IsPrimaryButtonEnabled = false
        };

        listBox.SelectionChanged += (_, _) =>
            dialog.IsPrimaryButtonEnabled = listBox.SelectedIndex >= 0;

        var dialogResult = await dialog.ShowAsync();

        return dialogResult == ContentDialogResult.Primary && listBox.SelectedIndex >= 0
            ? hits[listBox.SelectedIndex]
            : null;
    }
}
