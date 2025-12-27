using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using Xceed.Document.NET;
using Xceed.Words.NET;

namespace Reports
{
    public partial class ReservationForm
    {
        public ReservationForm()
        {
            InitializeComponent();
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            var name = Name.Text.Trim();
            var id = Id.Text.Trim();
            var car = Car.Text.Trim();
            var carId = CarId.Text.Trim();
            var address = Address.Text.Trim();
            var km = Km.Text.Trim();
            var reservation = Reservation.Text.Trim();
            var start = Start.Text.Trim();
            var end = End.Text.Trim();
            var toggle = ToggleOption.IsChecked == true ? "goto" : "autotel";

            var date = ExtractDate(end);

            var cost = CalculateCost(start, end, km, toggle).ToString(CultureInfo.InvariantCulture);


            // Downloads folder
            var downloadsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                "Downloads"
            );

            // File paths
            var docxPath = Path.Combine(downloadsPath, $"Reservation - {name}.docx");

            // Extract embedded template
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"Reports.{toggle}_reservation.docx";

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
                doc.ReplaceText(new StringReplaceTextOptions{
                        SearchValue = "<<Start>>", 
                        NewValue = start
                    }
                );
                doc.ReplaceText(new StringReplaceTextOptions{
                        SearchValue = "<<End>>", 
                        NewValue = end
                    }
                );
                doc.ReplaceText(new StringReplaceTextOptions{
                        SearchValue = "<<Car>>", 
                        NewValue = car
                    }
                );
                doc.ReplaceText(new StringReplaceTextOptions{
                        SearchValue = "<<CarId>>", 
                        NewValue = carId
                    }
                );
                doc.ReplaceText(new StringReplaceTextOptions{
                        SearchValue = "<<Address>>", 
                        NewValue = address
                    }
                );
                doc.ReplaceText(new StringReplaceTextOptions{
                        SearchValue = "<<Km>>", 
                        NewValue = km
                    }
                );
                doc.ReplaceText(new StringReplaceTextOptions
                    {
                        SearchValue = "<<Id>>",
                        NewValue = id
                    }
                );
                doc.ReplaceText(new StringReplaceTextOptions{
                        SearchValue = "<<Reservation>>", 
                        NewValue = reservation
                    }
                );
                doc.ReplaceText(new StringReplaceTextOptions{
                        SearchValue = "<<Cost>>", 
                        NewValue = cost
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
            Id.Text = "";
            Car.Text = "";
            CarId.Text = "";
            Address.Text = "";
            Start.Text = "";
            End.Text = "";
            Reservation.Text = "";
            Km.Text = "";
        }

        private static string ExtractDate(string end)
        {
            var date = end.Split(" ")[0];

            if (date.Contains(':'))
            {
                date = end.Split(" ")[1];
            }

            return date;
        }

        private static double CalculateCost(string start, string end, string km, string toggle)
        {
            DateTime startDt = ParseFlexibleDate(start);
            DateTime endDt   = ParseFlexibleDate(end);
            
            TimeSpan diff = endDt - startDt;
            
            var days = diff.Days;
            var hours = diff.Hours + (diff.Minutes / 60);
            var kmNum = double.Parse(km);
            
            var cost = 0.0;

            if (toggle == "goto")
            {
                cost = days * 150;
                cost += 1.3 * kmNum;

                if (hours >= 8)
                {
                    cost += 150;
                }
                else
                {
                    cost += hours * 15;
                }
            }
            else
            {
                days *= 24;
                hours += days;
                if (hours >= 3)
                {
                    cost += 99;
                    cost += 1.2 * kmNum;
                    hours -= 3;
                    cost += 20*hours;
                }
                else
                {
                    cost += (hours * 60) * 1.5;
                }
            }

            return cost;
        }
        private static DateTime ParseFlexibleDate(string input)
        {
            string[] formats =
            [
                "dd/MM/yyyy HH:mm",
                "HH:mm dd/MM/yyyy"
            ];

            return DateTime.ParseExact(
                input,
                formats,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None
            );
        }
    }
}
