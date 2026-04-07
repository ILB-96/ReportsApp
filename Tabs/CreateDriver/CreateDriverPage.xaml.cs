using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Reports.Data;
using Reports.Services;
using Reports.Services.ChromeSync;
using Reports.Services.Crm;
using Reports.Services.Drivers;
using Reports.Services.Export;
using Reports.Utilities;
using MessageBox = System.Windows.MessageBox;

namespace Reports.Tabs.CreateDriver;

public partial class CreateDriverPage : Page
{
    private readonly IDriverDraftService _driverDraftService;
    private readonly IDriverSubmissionService _driverSubmissionService;
    private readonly IDriversExportService _driversExportService;
    private readonly ICrmBrandResolver _brandResolver;
    private readonly IDriverPaths _driverPaths;
    private readonly PhonesRepository _phonesRepository;
    
    public ChromeSyncStore SyncStore { get; }
    public IReadOnlyList<string> ServiceTypes { get; }

    public CreateDriverView View { get; }
    public CreateDriverPage(
        ChromeSyncStore syncStore,
        ICrmBrandResolver brandResolver,
        IDriverPaths driverPaths,
        IDriverDraftService driverDraftService,
        IDriverSubmissionService driverSubmissionService,
        IDriversExportService driversExportService,
        PhonesRepository phonesRepository)
    {
        InitializeComponent();

        _brandResolver = brandResolver;
        _driverPaths = driverPaths;
        _driverDraftService = driverDraftService;
        _driverSubmissionService = driverSubmissionService;
        _driversExportService = driversExportService;
        _phonesRepository = phonesRepository;

        ServiceTypes = _brandResolver.ServiceTypes;
        SyncStore = syncStore;
        View = new CreateDriverView();
        
        DataContext = this;
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
                    $"זה ימחק את כל השורות בקובץ {_driverPaths.DriversFile(serviceType)}\nלהמשיך?",
                    "אישור מחיקה",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirm != MessageBoxResult.Yes)
                    return;

                await Task.Run(() => _driversExportService.ClearRows(excelPath, _driverPaths.DriversLastColToClear));            }

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
                var draft = await _driverDraftService.LoadDraftAsync(View.ToDraftRequest());

                var phoneExists = await _phonesRepository.ExistsAsync(draft.Phone, draft.ServiceType);

                if (phoneExists)
                {
                    var confirm = MessageBox.Show(
                        "Driver exists already, do you wish to proceed?",
                        "אישור המשך",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (confirm != MessageBoxResult.Yes)
                    {
                        View.ShowInput();
                        return;
                    }
                    
                }

                View.FillFromDraft(draft);
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
                await _phonesRepository.InsertAsync(submission.Phone, submission.ServiceType);
                
                View.ShowInput();
                await Overlay.ShowAsync(true, $"שורה נוספה לקובץ {result.DriversFileName}", 4000);
            }
        }
        catch (Exception ex)
        {
            await Overlay.ShowAsync(false, ex.ToString());
        }
    }
    

    private string GetExcelPath(string serviceType)
        => Path.Combine(_driverPaths.DriversFolderPath, _driverPaths.DriversFile(serviceType));
    

    private void PreviousPage_Click(object sender, RoutedEventArgs e) => View.ShowInput();
}
