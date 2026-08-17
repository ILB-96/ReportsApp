using System.IO;
using System.Windows;
using System.Windows.Controls;
using Reports.Services.Agreement;
using Reports.Services.ChromeSync;
using Reports.Services.Crm;
using Reports.Services.Templates;
using Reports.Utilities;

namespace Reports.Tabs.CreateAgreement
{
    public partial class CreateAgreement : Page
    {
        private readonly ITemplateCatalog _templateCatalog;
        private readonly ICrmBrandResolver _brandResolver;
        private readonly IAgreementDraftService _agreementDraftService;
        private readonly IAgreementSubmissionService _agreementSubmissionService;
    
        public ChromeSyncStore SyncStore { get; }
        
        public CreateAgreementView View { get; }

        public CreateAgreement(ITemplateCatalog templateCatalog,
            ICrmBrandResolver brandResolver,
            ChromeSyncStore syncStore,
            IAgreementDraftService agreementDraftService,
            IAgreementSubmissionService  agreementSubmissionService)
        {
            InitializeComponent();
            _templateCatalog = templateCatalog;
            _brandResolver =  brandResolver;
            SyncStore = syncStore;
            _agreementDraftService = agreementDraftService;
            _agreementSubmissionService = agreementSubmissionService;
            DataContext = this;
            
            View = new CreateAgreementView();
        }
        private async void Submit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (Loading.BeginScope("מיצא את פרטי החוזה... רגע סבלנות", "זה יכול לקחת עד כמה שניות..."))
                {
                    var draft = await _agreementDraftService.LoadDraftAsync(View.ToDraftRequest());
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
                using (Loading.BeginScope("מייצר חוזה... רגע סבלנות", "זה יכול לקחת עד כמה שניות..."))
                {
                    var brand = _brandResolver.ServiceTypeFromUrl(View.Url);
                    var submission = View.ToSubmission(brand);

                    await _agreementSubmissionService.SubmitAsync(submission);

                    View.ShowInput();
                    await Overlay.ShowAsync(true, $"Created agreement", 4000);
                }
            }
            catch (Exception ex)
            {
                await Overlay.ShowAsync(false, ex.ToString());
            }
        }
        private void PreviousPage_Click(object sender, RoutedEventArgs e) => View.ShowInput();
    }
}
