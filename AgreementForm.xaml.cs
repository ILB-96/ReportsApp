using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using Xceed.Document.NET;
using Xceed.Words.NET;

namespace Reports
{
    public partial class AgreementForm
    {
        public AgreementForm()
        {
            InitializeComponent();
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            var name = Name.Text.Trim();
            var date = Date.Text.Trim();
            var toggle = ToggleOption.IsChecked == true ? "goto" : "autotel";

            // Downloads folder
            var downloadsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads"
            );

            // File paths
            var docxPath = Path.Combine(downloadsPath, $"Agreement - {name}.docx");

            // Extract embedded template
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"Reports.{toggle}_agreement.docx";

            using (Stream? stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    MessageBox.Show("Template not found in resources. Check resource name.");
                    return;
                }

                // Copy embedded template to a temp file so DocX can load it
                var tempTemplatePath = Path.Combine(Path.GetTempPath(), "template.docx");

                using (var fileStream = File.Create(tempTemplatePath))
                {
                    stream.CopyTo(fileStream);
                }

                // Load with DocX
                var doc = DocX.Load(tempTemplatePath);

                // Replace placeholders
                doc.ReplaceText(new StringReplaceTextOptions
                    {
                        SearchValue = "<<Name>>",
                        NewValue = name
                    }
                );
                doc.ReplaceText(new StringReplaceTextOptions{
                        SearchValue = "<<Date>>", 
                        NewValue = date
                    }
                );

                try
                {
                    // Save updated Word file
                    doc.SaveAs(docxPath);
                }
                catch (IOException)
                {
                    MessageBox.Show("Please close the document and try again.",
                        "File In Use",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = docxPath,
                UseShellExecute = true
            });

            Name.Text = "";
            Date.Text = "";
        }
    }
}
