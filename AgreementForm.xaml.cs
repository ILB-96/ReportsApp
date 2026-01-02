
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

            LoadingOverlay.Visibility = Visibility.Visible;
            RootForm.IsEnabled = false;
            ToggleOption.IsEnabled = false;

            try
            {
                await Task.Run(() =>
                {
                    var assembly = Assembly.GetExecutingAssembly();
                    var resourceName = $"Reports.{toggle}_agreement.docx";

                    using Stream? stream = assembly.GetManifestResourceStream(resourceName);
                    if (stream == null)
                        throw new FileNotFoundException($"Template not found in resources: {resourceName}");

                    var tempTemplatePath = Path.Combine(Path.GetTempPath(), "agreement_template.docx");
                    using (var fileStream = File.Create(tempTemplatePath))
                        stream.CopyTo(fileStream);

                    using (var doc = DocX.Load(tempTemplatePath))
                    {
                        TokenMerge.ReplaceTokens(doc, fields);

                        try
                        {
                            doc.SaveAs(docxPath);
                        }
                        catch (IOException ex)
                        {
                            throw new IOException("Please close the document and try again.", ex);
                        }
                        finally
                        {
                            try { File.Delete(tempTemplatePath); } catch { /* ignore */ }
                        }
                    }
                });

                Process.Start(new ProcessStartInfo
                {
                    FileName = docxPath,
                    UseShellExecute = true
                });
                
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
