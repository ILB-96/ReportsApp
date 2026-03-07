using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Reports.Services;
using Reports.Utilities;

namespace Reports.Tabs
{
    public partial class AgreementForm : Page
    {
        private readonly AppConfig _config;
        public AgreementForm() : this(App.Services.GetRequiredService<AppConfig>(), App.Services.GetRequiredService<ChromeTabsStore>())
        {
        }

        public AgreementForm(AppConfig config, ChromeTabsStore requiredService)
        {
            InitializeComponent();
            _config = config;
            DataContext = _config;
        }
        private async void Submit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await Loading.ShowAsync("יוצר את ההסכם... רגע סבלנות", "זה יכול לקחת עד כמה שניות...");
                RootForm.IsEnabled = false;
                TogglePanel.IsEnabled = false;
                
                var toggle = TogglePanel.IsChecked ? "goto" : "autotel";

                var options = new FieldCollectorOptions
                {
                    // Hebrew yes/no example for booleans:
                    BooleanFormatter = b => b ? "Yes" : "No",
                    // Date format example:
                    DateFormatter = dt => dt.ToString("dd/MM/yyyy"),
                };
                var fields = FormFieldCollector.CollectFields(RootForm, options);


                var downloadsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Downloads"
                );

                fields.TryGetValue("Name", out var nameValue);
                var safeName = FileNameUtils.SanitizeFileName(nameValue ?? string.Empty);

                var docxPath = Path.Combine(downloadsPath, $"Agreement - {safeName}.docx");
                var resourceName = _config.AgreementTemplate(toggle);
                await DocxTemplateGenerator.GenerateFromEmbeddedAsync(
                    embeddedResourceName: resourceName,
                    outputPath: docxPath,
                    tokens: fields);
                await DocxTemplateGenerator.SaveToPdf(docxPath);

                foreach (var tb in FormFieldCollector.FindVisualChildren<TextBox>(RootForm))
                    tb.Clear();
                
            }
            catch (IOException ex)
            {
                MessageBox.Show(ex.Message, "File In Use",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            finally
            {
                await Loading.HideAsync();
                RootForm.IsEnabled = true;
                TogglePanel.IsEnabled = true;
            }
        }
    }
}
