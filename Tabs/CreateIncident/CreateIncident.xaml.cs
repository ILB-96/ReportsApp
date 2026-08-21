using System.Windows;
using System.Windows.Controls;
using Reports.Services.ChromeSync;
using Reports.Services.Crm;
using Reports.Services.Drivers;
using Reports.Services.Incident;
using Reports.Tabs.CreateDriver;

namespace Reports.Tabs.CreateIncident;

public partial class CreateIncident : Page
{
    private readonly ICrmBrandResolver _brandResolver;
    private readonly IIncidentDraftService _incidentDraftService;
    private readonly IIncidentSubmissionService _incidentSubmissionService;
    
    public ChromeSyncStore SyncStore { get; }
    public IReadOnlyList<string> ServiceTypes { get; }
    public CreateIncidentView View { get; }
    
    public CreateIncident(
        ChromeSyncStore syncStore,
        ICrmBrandResolver brandResolver,
        IIncidentDraftService incidentDraftService,
        IIncidentSubmissionService incidentSubmissionService)
    {
        InitializeComponent();
        
        _brandResolver = brandResolver;
        _incidentDraftService = incidentDraftService;
        _incidentSubmissionService = incidentSubmissionService;
        ServiceTypes = _brandResolver.ServiceTypes;
        
        SyncStore = syncStore;
        
        View = new CreateIncidentView();
        
        DataContext = this;
    }
    
    private async void Submit_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using var scope = Loading.BeginScope("מיצא את פרטי הקייס... רגע סבלנות", "זה יכול לקחת עד כמה שניות...", cancelable: true);
            var ct = scope.Token;
            var draft = await _incidentDraftService.LoadDraftAsync(View.ToDraftRequest(), ct);
            
            ct.ThrowIfCancellationRequested();
            if (draft != null) View.FillFromDraft(draft);
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
    private async void SendFinal_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using (Loading.BeginScope("מייצר קייס... רגע סבלנות", "זה יכול לקחת עד כמה שניות..."))
            {
                var brand = _brandResolver.ServiceTypeFromUrl(View.Url);
                var submission = View.ToSubmission();
                //
                var result = await _incidentSubmissionService.SubmitAsync(submission, View.ToDraftRequest());
                
                View.Reset();
                View.ShowInput();
            }
        }
        catch (Exception ex)
        {
            await Overlay.ShowAsync(false, ex.ToString());
        }
    }
    private void PreviousPage_Click(object sender, RoutedEventArgs e) => View.ShowInput();

}