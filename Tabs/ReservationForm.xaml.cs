using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using Reports.Services;
using Reports.Utilities;
using Xceed.Words.NET;
// <-- use the utilities

namespace Reports.Tabs
{
    public partial class ReservationForm : Page
    {
        private readonly AppConfig _config;
        public ReservationForm() : this(App.Services.GetRequiredService<AppConfig>(), App.Services.GetRequiredService<ChromeTabsStore>())
        {
        }

        public ReservationForm(AppConfig config, ChromeTabsStore requiredService)
        {
            InitializeComponent();
            _config = config;
            DataContext = _config;
        }

        private async void Submit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // UI busy
                LoadingOverlay.Visibility = Visibility.Visible;
                RootForm.IsEnabled = false;
                var isGoto = TogglePanel.IsChecked; 
                var templateToggle = isGoto? "goto" : "autotel";

                // Collect all raw fields from the form container
                // (Assumes you named the container that holds inputs "RootForm" in XAML)
                var options = new FieldCollectorOptions
                {
                    // optional Hebrew formatting:
                    BooleanFormatter = b => b ? "כן" : "לא",
                    DateFormatter = dt => dt.ToString("dd/MM/yyyy", new CultureInfo("he-IL")),
                };

                var fields = FormFieldCollector.CollectFields(RootForm, options);


                // Apply form-specific computed values / overrides
                ReservationTransforms.Apply(ref fields, isGoto);

                // File target
                var downloadsPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Downloads");
                Directory.CreateDirectory(downloadsPath);

                fields.TryGetValue("Name", out var nameValue);
                var safeName = FileNameUtils.SanitizeFileName(nameValue ?? string.Empty);
                var docxPath = Path.Combine(downloadsPath, $"Reservation - {safeName}.docx");


                await Task.Run(() =>
                {
                    // Extract embedded template
                    var assembly = Assembly.GetExecutingAssembly();
                    var resourceName = _config.ReservationTemplate(templateToggle);

                    using Stream? stream = assembly.GetManifestResourceStream(resourceName);
                    if (stream == null)
                        throw new FileNotFoundException($"Template not found in resources: {resourceName}");

                    var tempTemplatePath = Path.Combine(Path.GetTempPath(), $"template_{Guid.NewGuid():N}.docx");
                    using (var fileStream = File.Create(tempTemplatePath))
                        stream.CopyTo(fileStream);

                    using (var doc = DocX.Load(tempTemplatePath))
                    {
                        // Single pass: replace all tokens from dictionary
                        TokenMerge.ReplaceTokens(doc, fields);

                        doc.SaveAs(docxPath);
                    }

                    try
                    {
                        File.Delete(tempTemplatePath);
                    }
                    catch
                    {
                        /* ignore */
                    }
                });

                Process.Start(new ProcessStartInfo
                {
                    FileName = docxPath,
                    UseShellExecute = true
                });

                // Optional: clear text inputs
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
            }

        }
    }
}
