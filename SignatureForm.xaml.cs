using System.Windows.Controls;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using Xceed.Document.NET;
using Xceed.Words.NET;

namespace Reports;

public partial class SignatureForm
{
    public SignatureForm()
    {
        InitializeComponent();
    }
            // Allow only numbers
        private void NumberOnly(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !MyRegex().IsMatch(e.Text);
        }

        private static void ReplacePlaceholder(DocX doc, string placeholder, string value)
        {
            doc.ReplaceText(new StringReplaceTextOptions
                {
                    SearchValue = placeholder,
                    NewValue = value
                }
            );
        }



        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            var report = Report.Text.Trim();
            var id = Id.Text.Trim();
            var car = Car.Text.Trim().Replace("-", "");
            var license = License.Text.Trim();
            var name = Name.Text.Trim();
            var address = Address.Text.Trim();
            var forMunicipality = For.Text.Trim();
            var toggle = ToggleOption.IsChecked == true ? "goto" : "autotel";

            // Downloads folder
            var downloadsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads"
            );

            // File paths
            var docxPath = Path.Combine(downloadsPath, $"Signature - {name}.docx");

            // Extract embedded template to memory
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"Reports.{toggle}_autograph.docx"; 

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
                
                
                ReplacePlaceholder(doc, "<<For>>", forMunicipality);
                ReplacePlaceholder(doc, "<<Report>>", report);
                ReplacePlaceholder(doc, "<<Id>>", id);
                ReplacePlaceholder(doc, "<<Car>>", car);
                ReplacePlaceholder(doc, "<<License>>", license);
                ReplacePlaceholder(doc, "<<Name>>", name);
                ReplacePlaceholder(doc, "<<Address>>", address);
                
                try
                {
                    // Save updated Word file
                    doc.SaveAs(docxPath);
                }
                catch (IOException)
                {
                    MessageBox.Show("Please close 'Updated.docx' and try again.", "File In Use", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            
            
    
            // Open the generated file
            Process.Start(new ProcessStartInfo
            {
                FileName = docxPath,
                UseShellExecute = true
            });
            
            
            Report.Text = "";
            Id.Text = "";
            Car.Text = "";
            License.Text = "";
            Name.Text = "";
            Address.Text = "";
        }
        
        [GeneratedRegex("^[0-9-]+$")]
        private static partial Regex MyRegex();
    
}

