using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Reports.Services.Email;
using Reports.Services.Email.CustomerRequests;

namespace Reports.Tabs;

public partial class CreateCustomerRequest : Page
{
    private readonly IAddressParser _addressParser;
    private readonly IEmailDraftBuilder<CustomerRequestEmailModel> _draftBuilder;
    private readonly IEmailComposerService _emailComposer;

    public CreateCustomerRequest(
        IAddressParser addressParser,
        IEmailDraftBuilder<CustomerRequestEmailModel> draftBuilder,
        IEmailComposerService emailComposer)
    {
        InitializeComponent();
        _addressParser = addressParser;
        _draftBuilder = draftBuilder;
        _emailComposer = emailComposer;
    }

    private void NumberOnly(object sender, TextCompositionEventArgs e)
    {
        e.Handled = !NumbersOnlyRegex().IsMatch(e.Text);
    }

    [GeneratedRegex("^[0-9-]+$")]
    private static partial Regex NumbersOnlyRegex();

    private void Submit_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            RootForm.IsEnabled = false;

            var company = GetSelectedCompany();
            if (company is null)
            {
                MessageBox.Show(
                    "יש לבחור חברה לפני השליחה.",
                    "Validation Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            var model = new CustomerRequestEmailModel(
                Company: company,
                FullName: Name.Text.Trim(),
                IdNumber: Id.Text.Trim(),
                Email: Email.Text.Trim(),
                Address: _addressParser.Parse(AddressName.Text.Trim()),
                CarNumber: Car.Text.Trim().Replace("-", ""),
                StartTime: StartTime.Text.Trim(),
                EndTime: EndTime.Text.Trim());

            var draft = _draftBuilder.Build(model);
            _emailComposer.OpenDraft(draft);

            ClearForm();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Failed to open the email draft: {ex.Message}",
                "Email Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            LoadingOverlay.Visibility = Visibility.Collapsed;
            RootForm.IsEnabled = true;
        }
    }

    private string? GetSelectedCompany() =>
        RB_Shutuf.IsChecked == true ? "שיתוף" :
        RB_Mobility.IsChecked == true ? "מוביליטי" :
        RB_Colmobil.IsChecked == true ? "כולמוביל" :
        null;

    private void ClearForm()
    {
        Name.Text = string.Empty;
        Id.Text = string.Empty;
        AddressName.Text = string.Empty;
        Email.Text = string.Empty;
        Car.Text = string.Empty;
        StartTime.Text = string.Empty;
        EndTime.Text = string.Empty;
    }
}