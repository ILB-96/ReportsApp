using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Reports.Services.Drivers;

namespace Reports.Tabs.CreateDriver;

public sealed partial class CreateDriverView : INotifyPropertyChanged
{
    private string _url = string.Empty;
    private string _serviceType = string.Empty;
    private string _reportStartDate = string.Empty;
    private string _reportEndDate = string.Empty;
    private string _carLicense = string.Empty;
    private string _reservationNumber = string.Empty;
    private string _reportNumber = string.Empty;
    private string _accountFullName = string.Empty;
    private string _driverId = string.Empty;
    private string _driverLicense = string.Empty;
    private string _email = string.Empty;
    private string _phone = string.Empty;
    private string _address = string.Empty;
    private string _house = string.Empty;
    private string _city = string.Empty;
    private string _postalCode = string.Empty;
    private string _createdOn = string.Empty;
    private string _licenseLink = string.Empty;
    private string _passportLink = string.Empty;

    private Visibility _inputPanelVisibility = Visibility.Visible;
    private Visibility _dataPanelVisibility = Visibility.Collapsed;

    public string Url { get => _url; set => SetField(ref _url, value); }
    public string ServiceType { get => _serviceType; set => SetField(ref _serviceType, value); }
    public string ReportStartDate { get => _reportStartDate; set => SetField(ref _reportStartDate, value); }
    public string ReportEndDate { get => _reportEndDate; set => SetField(ref _reportEndDate, value); }
    public string CarLicense { get => _carLicense; set => SetField(ref _carLicense, value); }
    public string ReservationNumber { get => _reservationNumber; set => SetField(ref _reservationNumber, value); }
    public string ReportNumber { get => _reportNumber; set => SetField(ref _reportNumber, value); }
    public string AccountFullName { get => _accountFullName; set => SetField(ref _accountFullName, value); }
    public string DriverId { get => _driverId; set => SetField(ref _driverId, value); }
    public string DriverLicense { get => _driverLicense; set => SetField(ref _driverLicense, value); }
    public string Email { get => _email; set => SetField(ref _email, value); }
    public string Phone { get => _phone; set => SetField(ref _phone, value); }
    public string Address { get => _address; set => SetField(ref _address, value); }
    public string House { get => _house; set => SetField(ref _house, value); }
    public string City { get => _city; set => SetField(ref _city, value); }
    public string PostalCode { get => _postalCode; set => SetField(ref _postalCode, value); }
    public string CreatedOn { get => _createdOn; set => SetField(ref _createdOn, value); }
    public string LicenseLink { get => _licenseLink; set => SetField(ref _licenseLink, value); }
    public string PassportLink { get => _passportLink; set => SetField(ref _passportLink, value); }

    public Visibility InputPanelVisibility
    {
        get => _inputPanelVisibility;
        set => SetField(ref _inputPanelVisibility, value);
    }

    public Visibility DataPanelVisibility
    {
        get => _dataPanelVisibility;
        set => SetField(ref _dataPanelVisibility, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void ShowInput()
    {
        InputPanelVisibility = Visibility.Visible;
        DataPanelVisibility = Visibility.Collapsed;
    }

    public void ShowData()
    {
        InputPanelVisibility = Visibility.Collapsed;
        DataPanelVisibility = Visibility.Visible;
    }

    public void FillFromDraft(CreateDriverDraft draft)
    {
        ServiceType = draft.ServiceType;
        ReportStartDate = draft.ReportStartDate;
        ReportEndDate = draft.ReportEndDate;
        CarLicense = draft.CarLicense;
        ReservationNumber = draft.ReservationNumber;
        ReportNumber = draft.ReportNumber;
        AccountFullName = draft.AccountFullName;
        DriverId = draft.DriverId;
        DriverLicense = draft.DriverLicense;
        Email = draft.Email;
        Phone = draft.Phone;
        Address = draft.Address;
        House = draft.House;
        City = draft.City;
        PostalCode = draft.PostalCode;
        CreatedOn = draft.CreatedOn;
        LicenseLink = draft.LicenseLink;
        PassportLink = draft.PassportLink;
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return;

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
    
    public CreateDriverRequest ToDraftRequest()
    {
        return new CreateDriverRequest
        {
            Url = Url.Trim()
        };
    }

    public DriverSubmission ToSubmission(string brand)
    {
        return new DriverSubmission
        {
            Brand = brand,
            ServiceType = ServiceType.Trim(),
            CarLicense = CarLicense,
            AccountFullName = AccountFullName,
            DriverId = DriverId,
            Phone = Phone,
            ReportStartDate = ReportStartDate,
            ReportEndDate = ReportEndDate,
            DriverLicense = DriverLicense,
            Address = Address,
            House = House,
            City = City,
            Email = Email,
            PostalCode = PostalCode,
            ReservationNumber = ReservationNumber,
            CreatedOn = CreatedOn,
            LicenseLink = LicenseLink,
            PassportLink = PassportLink
        };
    }
}