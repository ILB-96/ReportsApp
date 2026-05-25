using System.Windows;
using System.Windows.Controls;
using Reports.Services.BetterwayApi;
using Reports.Services.ChromeSync;
using Reports.Services.Crm;
using Reports.Services.Drivers;
using MessageBox = System.Windows.MessageBox;

namespace Reports.Tabs.CreateDriver;

public partial class CreateDriverPage : Page
{
    private readonly IDriverDraftService _driverDraftService;
    private readonly IDriverSubmissionService _driverSubmissionService;
    private readonly ICrmBrandResolver _brandResolver;
    private readonly IDriverPaths _driverPaths;
    private readonly IBetterwayDriverSearch _betterwayDriverSearch;
    
    public ChromeSyncStore SyncStore { get; }
    public IReadOnlyList<string> ServiceTypes { get; }

    public CreateDriverView View { get; }
    public CreateDriverPage(
        ChromeSyncStore syncStore,
        ICrmBrandResolver brandResolver,
        IDriverPaths driverPaths,
        IDriverDraftService driverDraftService,
        IDriverSubmissionService driverSubmissionService,
        IBetterwayDriverSearch betterwayDriverSearch)
    {
        InitializeComponent();

        _brandResolver = brandResolver;
        _driverPaths = driverPaths;
        _driverDraftService = driverDraftService;
        _driverSubmissionService = driverSubmissionService;
        _betterwayDriverSearch = betterwayDriverSearch;

        ServiceTypes = _brandResolver.ServiceTypes;
        SyncStore = syncStore;
        View = new CreateDriverView();
        
        DataContext = this;
    }
    

    private async void Submit_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using (Loading.BeginScope("מיצא את פרטי הנהג... רגע סבלנות", "זה יכול לקחת עד כמה שניות..."))
            {
                var draft = await _driverDraftService.LoadDraftAsync(View.ToDraftRequest());
                
                View.FillFromDraft(draft);
                var result = await _betterwayDriverSearch.SearchAllProfilesAsync(draft.Phone);

                if (result.FirstMatch is not null)
                {
                    // Real signal: same customer exists in multiple profiles.
                    var profilesList = string.Join(", ", result.ProfilesWithMatch);
                    MessageBox.Show(
                        $"Driver found in: {profilesList}",
                        "Driver found!",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    View.FillFromDriver(result.FirstMatch);
                }

                View.ShowData();
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
            using (Loading.BeginScope("מייצר נהג... רגע סבלנות", "זה יכול לקחת עד כמה שניות..."))
            {
                var brand = _brandResolver.ServiceTypeFromUrl(View.Url);
                var submission = View.ToSubmission(brand);

                var result = await _driverSubmissionService.SubmitAsync(submission);

                View.ShowInput();
                await Overlay.ShowAsync(true, $"Create driver response {result.ResponseBody}", 4000);
            }
        }
        catch (Exception ex)
        {
            await Overlay.ShowAsync(false, ex.ToString());
        }
    }
    

    private void PreviousPage_Click(object sender, RoutedEventArgs e) => View.ShowInput();
}
