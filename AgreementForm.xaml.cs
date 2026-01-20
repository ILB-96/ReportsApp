
using System.Diagnostics;
using System.IO;
using System.Reflection;

using Reports.Utilities;
using System.Windows;
using System.Windows.Controls;
using Xceed.Document.NET;
using Xceed.Words.NET;

namespace Reports
{
    public partial class AgreementForm : Page
    {
        public AgreementForm()
        {
            InitializeComponent();
        }

        private async void Submit_Click(object sender, RoutedEventArgs e)
        {
            var toggle = ToggleOption.IsChecked == true ? "goto" : "autotel";
            
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
            var resourceName = $"Reports.{toggle}_agreement.docx";
            await DocxTemplateGenerator.GenerateFromEmbeddedAsync(
                embeddedResourceName: resourceName,
                outputPath: docxPath,
                tokens: fields);
            DocxTemplateGenerator.OpenInShell(docxPath);
            
            LoadingOverlay.Visibility = Visibility.Visible;
            RootForm.IsEnabled = false;
            ToggleOption.IsEnabled = false;
            try
            {
                foreach (var tb in FormFieldCollector.FindVisualChildren<TextBox>(RootForm))
                    tb.Clear();
            }
            catch (IOException ioEx)
            {
                MessageBox.Show(ioEx.Message, "File In Use",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
                RootForm.IsEnabled = true;
                ToggleOption.IsEnabled = true;
            }
        }
    }
}
