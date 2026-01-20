using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace Reports;

public partial class CreateCustomerRequest
{
    public CreateCustomerRequest()
    {
        InitializeComponent();
    }
    private void NumberOnly(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !MyRegex().IsMatch(e.Text);
    }
    [GeneratedRegex("^[0-9-]+$")]
    private static partial Regex MyRegex();

    
    private void Submit_Click(object sender, RoutedEventArgs e)
    {
        
        var name = Name.Text.Trim();
        var id = Id.Text.Trim();
        var email = Email.Text.Trim();
        var addressName = AddressName.Text.Trim();
        var car = Car.Text.Trim().Replace("-", "");
        var startTime = StartTime.Text.Trim();
        var endTime = EndTime.Text.Trim();
        
        string? selected;

        if (RB_Shutuf.IsChecked == true)
            selected = "שיתוף";
        else if (RB_Mobility.IsChecked == true)
            selected = "מוביליטי";
        else if (RB_Colmobil.IsChecked == true)
            selected = "כולמוביל";
        else
            selected = null; // nothing selected

        
        var address = ParseAddress(addressName);

        try
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            RootForm.IsEnabled = false;

            var outlookType = Type.GetTypeFromProgID("Outlook.Application");
            if (outlookType == null)
            {
                MessageBox.Show("Outlook is not installed on this computer.", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            dynamic? outlookApp = Activator.CreateInstance(outlookType);
            var mail = outlookApp!.CreateItem(0); // 0 = olMailItem

            mail.To = "info@betterway.co.il";
            mail.CC = "yifat.vio@gotoglobal.com";
            mail.Subject = $"הקמת לקוח - {selected} - {id}";

            mail.Display();
            var customBody = $@"
                        <html>
                          <body style='font-family: Arial, sans-serif; font-size: 14.5px; direction: rtl; text-align: right; line-height: 1.6;'>
                            <p style='margin: 0 0 12px 0;'>היי,</p>

                            <p style='margin: 0;'>
                              ח.פ חברה: {selected}<br>
                              שם מלא: {name}<br>
                              תעודת זהות: {id}<br>
                              כתובת מייל: {email}<br>
                              עיר: {address.City}<br>
                              רחוב: {address.Street}<br>
                              מספר רחוב: {address.StreetNumber}<br>
                              מספר דירה: {address.ApartmentNumber}<br>
                              מיקוד: {address.ZipCode}<br><br>
                              מספר רכב לשיוך: {car}<br>
                              תאריך ושעת התחלה: {startTime}<br>
                              תאריך ושעת סיום: {endTime}
                            </p>
                          </body>
                        </html>";


            mail.HTMLBody = customBody + mail.HTMLBody;

            Name.Text = "";
            Id.Text = "";
            AddressName.Text = "";
            Email.Text = "";
            Car.Text = "";
            StartTime.Text = "";
            EndTime.Text = "";
        }
        catch (Exception ex)
        {
            MessageBox.Show("Error launching Outlook: " + ex.Message);
        }
        finally
        {
            LoadingOverlay.Visibility = Visibility.Hidden;
            RootForm.IsEnabled = true;
        }


    }

    public class AddressInfo
    {
        public string ZipCode { get; set; } = "";
        public string Street { get; set; } = "";
        public string StreetNumber { get; set; } = "";
        public string ApartmentNumber { get; set; } = "";
        public string City { get; set; } = "";
    }

    private static AddressInfo ParseAddress(string input)
    {
        var result = new AddressInfo();

        if (string.IsNullOrWhiteSpace(input))
            return result;

        var parts = input.Split(',')
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrEmpty(p))
            .ToArray();

        // Detect if first part is ZIP (digits only)
        var index = 0;

        if (index < parts.Length)
        {
            // Street name + number
            var match = MyRegex1().Match(parts[index]);
            if (match.Success)
            {
                result.Street = match.Groups[1].Value.Trim();
                result.StreetNumber = match.Groups[2].Value.Trim();
            }
            else
            {
                result.Street = parts[index];
            }
            index++;
        }

        if (index < parts.Length)
            result.ApartmentNumber = parts[index++];

        if (index < parts.Length)
            result.City = parts[index++];
        
        if (index < parts.Length)
            result.ZipCode = parts[index];

        return result;
    }

    [GeneratedRegex(@"^(.*?)[\s]+(\d+)$")]
    private static partial Regex MyRegex1();
}