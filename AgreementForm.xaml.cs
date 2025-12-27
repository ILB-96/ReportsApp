
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
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
            var name = Name.Text.Trim();
            var date = Date.Text.Trim();
            var toggle = ToggleOption.IsChecked == true ? "goto" : "autotel";

            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(date))
            {
                MessageBox.Show("נא למלא שם ותאריך.");
                return;
            }

            var downloadsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads"
            );
            var docxPath = Path.Combine(downloadsPath, $"Agreement - {name}.docx");

            LoadingOverlay.Visibility = Visibility.Visible;
            SubmitButton.IsEnabled = false;
            Name.IsEnabled = false;
            Date.IsEnabled = false;
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

                    var tempTemplatePath = Path.Combine(Path.GetTempPath(), "template.docx");
                    using (var fileStream = File.Create(tempTemplatePath))
                        stream.CopyTo(fileStream);

                    using (var doc = DocX.Load(tempTemplatePath))
                    {
                        // Make sure the SearchValue matches the literal text in your template
                        doc.ReplaceText(new StringReplaceTextOptions
                        {
                            SearchValue = "<<Name>>",
                            NewValue = name
                        });
                        doc.ReplaceText(new StringReplaceTextOptions
                        {
                            SearchValue = "<<Date>>",
                            NewValue = date
                        });

                        try
                        {
                            doc.SaveAs(docxPath);
                        }
                        catch (IOException ex)
                        {
                            throw new IOException("Please close the document and try again.", ex);
                        }
                    }
                });

                Process.Start(new ProcessStartInfo
                {
                    FileName = docxPath,
                    UseShellExecute = true
                });

                Name.Text = string.Empty;
                Date.Text = string.Empty;
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
                SubmitButton.IsEnabled = true;
                Name.IsEnabled = true;
                Date.IsEnabled = true;
                ToggleOption.IsEnabled = true;
            }
        }
    }
}
