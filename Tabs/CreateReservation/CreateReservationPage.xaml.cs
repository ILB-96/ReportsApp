using System.Windows;
using System.Windows.Controls;
using DocumentFormat.OpenXml.Drawing.Charts;
using Reports.Services.BetterwayApi;
using Reports.Services.ChromeSync;
using Reports.Services.Crm;
using Reports.Services.Drivers;
using Wpf.Ui.Controls;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using Thickness = System.Windows.Thickness;

namespace Reports.Tabs.CreateReservation;

public partial class CreateReservationPage : Page
{
    private readonly IDriverDraftService _driverDraftService;
    private readonly IDriverSubmissionService _driverSubmissionService;
    private readonly ICrmBrandResolver _brandResolver;
    private readonly IBetterwayDriverSearch _betterwayDriverSearch;
    
    public ChromeSyncStore SyncStore { get; }
    public IReadOnlyList<string> ServiceTypes { get; }

    public CreateReservationView View { get; }
    public CreateReservationPage(
        ChromeSyncStore syncStore,
        ICrmBrandResolver brandResolver,
        IDriverDraftService driverDraftService,
        IDriverSubmissionService driverSubmissionService,
        IBetterwayDriverSearch betterwayDriverSearch)
    {
        InitializeComponent();

        _brandResolver = brandResolver;
        _driverDraftService = driverDraftService;
        _driverSubmissionService = driverSubmissionService;
        _betterwayDriverSearch = betterwayDriverSearch;

        ServiceTypes = _brandResolver.ServiceTypes;
        SyncStore = syncStore;
        View = new CreateReservationView();
        
        DataContext = this;
    }
    

    private async void Submit_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using (Loading.BeginScope("מיצא את פרטי הנהג... רגע סבלנות", "זה יכול לקחת עד כמה שניות..."))
            {
                var (draft, reservation) = await _driverDraftService.LoadDraftAsync(View.ToDraftRequest());
                View.FillFromReservation(reservation);
                View.FillFromDraft(draft);
                
                var result = await _betterwayDriverSearch.SearchAllProfilesAsync(draft.Phone);
                DriverSearchHit? chosen = null;
                switch (result.AllHits.Count)
                {
                    case 1:
                    {
                        chosen = result.AllHits[0];
                        var profilesList = string.Join(", ", result.ProfilesWithMatch);
                        MessageBox.Show(
                            $"Driver found in: {profilesList}",
                            "Driver found!",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);
                        break;
                    }
                    case > 1:
                    {
                        var picked = await PickDriverAsync(result.AllHits);
                        if (picked is null) return; // user cancelled
                        chosen = picked;
                        break;
                    }
                }
                
                if (chosen is not null)
                {
                    View.FillFromDriver(chosen.Driver);
                }
                View.ShowData();
            }
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
            using (Loading.BeginScope("מייצר נהג... רגע סבלנות", "זה יכול לקחת עד כמה שניות..."))
            {
                var brand = _brandResolver.ServiceTypeFromUrl(View.Url);
                var submission = View.ToSubmission(brand);
                var reservation = View.ToReservation(brand);

                var result = await _driverSubmissionService.SubmitAsync(submission, reservation, false);

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
