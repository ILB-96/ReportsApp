using System.Windows.Controls;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using Reports.Utilities;
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
        
        private async void Submit_Click(object sender, RoutedEventArgs e)
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            RootForm.IsEnabled = false;
            
            var toggle = ToggleOption.IsChecked == true ? "goto" : "autotel";
            
            var options = new FieldCollectorOptions
            {
                // optional Hebrew formatting:
                BooleanFormatter = b => b ? "כן" : "לא",
                DateFormatter    = dt => dt.ToString("dd/MM/yyyy", new CultureInfo("he-IL")),
            };

            var fields = FormFieldCollector.CollectFields(RootForm, options);

            // Downloads folder
            var downloadsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads"
            );
            Directory.CreateDirectory(downloadsPath);

            fields.TryGetValue("Name", out var nameValue);
            var safeName = FileNameUtils.SanitizeFileName(nameValue ?? string.Empty);
            var docxPath = Path.Combine(downloadsPath, $"Signature - {safeName}.docx");

            try
            {
                await Task.Run(() =>
                {
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
                        var tempTemplatePath = Path.Combine(Path.GetTempPath(), "signature_template.docx");
                        using (var fileStream = File.Create(tempTemplatePath))
                        {
                            stream.CopyTo(fileStream);
                        }

                        // Load with DocX
                        var doc = DocX.Load(tempTemplatePath);


                        TokenMerge.ReplaceTokens(doc, fields);

                        try
                        {
                            // Save updated Word file
                            doc.SaveAs(docxPath);
                        }
                        catch (IOException)
                        {
                            MessageBox.Show("Please close 'Updated.docx' and try again.", "File In Use",
                                MessageBoxButton.OK,
                                MessageBoxImage.Warning);
                            return;
                        }
                        finally
                        {
                            try
                            {
                                File.Delete(tempTemplatePath);
                            }
                            catch
                            {
                                /* ignore */
                            }
                        }
                    }
                });


                // Open the generated file
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
            }

        }
        
        [GeneratedRegex("^[0-9-]+$")]
        private static partial Regex MyRegex();
    
}

