using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using Reports.Services.BetterwayApi;
using Reports.Services.Drivers;
using Reports.Services.Reservation;

namespace Reports.Tabs.CreateReservation;

public sealed partial class CreateReservationView : INotifyPropertyChanged
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
    private string _contractLink = string.Empty;
    private string _customerLink = string.Empty;
    private string _pickupLink = string.Empty;
    private string _returnLink = string.Empty;
    
    private string _reservationEndDate = string.Empty;
    private string _originType = string.Empty;
    private string _carType = string.Empty;
    private string _originAddress = string.Empty;
    private string _distanceKm = string.Empty;
    private decimal _reservationCost = 0;
    


    private Visibility _inputPanelVisibility = Visibility.Visible;
    private Visibility _dataPanelVisibility = Visibility.Collapsed;
    private Visibility _reservationPanelVisibility = Visibility.Collapsed;

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
    public string ContractLink { get => _contractLink; set => SetField(ref _contractLink, value); }
    public string CustomerLink { get => _customerLink; set => SetField(ref _customerLink, value); }
    public string PickupLink { get => _pickupLink; set => SetField(ref _pickupLink, value); }
    public string ReturnLink { get => _returnLink; set => SetField(ref _returnLink, value); }
    
    public string ReservationEndDate { get => _reservationEndDate; set => SetField(ref _reservationEndDate, value); }
    public string OriginType { get => _originType; set => SetField(ref _originType, value); }
    public string CarType { get => _carType; set => SetField(ref _carType, value); }
    public string OriginAddress { get => _originAddress; set => SetField(ref _originAddress, value); }
    public string DistanceKm { get => _distanceKm; set => SetField(ref _distanceKm, value); }
    public decimal ReservationCost { get => _reservationCost; set => SetField(ref _reservationCost, value); }
    

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
    
    public Visibility ReservationPanelVisibility
    {
        get => _reservationPanelVisibility;
        set => SetField(ref _reservationPanelVisibility, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void ShowInput()
    {
        InputPanelVisibility = Visibility.Visible;
        DataPanelVisibility = Visibility.Collapsed;
        ReservationPanelVisibility = Visibility.Collapsed;
    }

    public void ShowData()
    {
        InputPanelVisibility = Visibility.Collapsed;
        DataPanelVisibility = Visibility.Visible;
        ReservationPanelVisibility = Visibility.Collapsed;
    }
    public void ShowReservation()
    {
        InputPanelVisibility = Visibility.Collapsed;
        DataPanelVisibility = Visibility.Collapsed;
        ReservationPanelVisibility = Visibility.Visible;
    }
    public void FillFromDriver(BetterwayDriver driver)
    {
        AccountFullName = driver.Name;
        DriverId = driver.IdNumber ?? DriverId;
        DriverLicense = driver.LicenseNumber ?? DriverLicense;
        Email = driver.Email ?? Email;
        Phone = driver.PhoneNumber ?? Phone;
        Address = driver.Street ?? Address;
        House = driver.HouseNumber ?? House;
        City = driver.City ?? City;
        PostalCode = driver.ZipCode ?? PostalCode;
    }
    public void FillFromReservation(ReservationReceipt? reservation)
    {
        if (reservation is null) return;
        ReportStartDate = reservation.ReservationStartTime ?? ReportStartDate;
        ReservationEndDate = reservation.ReservationEndTime ?? ReservationEndDate;
        OriginAddress = reservation.OriginAddress ?? OriginAddress;
        ReservationCost = reservation.ReservationCost;
        DistanceKm = reservation.DistanceKm ?? DistanceKm;
        CarType = reservation.CarType ?? CarType;
    }

    public void FillFromDraft(CreateDriverDraft draft)
    {
        ServiceType = draft.ServiceType;
        ReportStartDate = String.IsNullOrWhiteSpace(ReportStartDate) ? draft.ReportStartDate : ReportStartDate;
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
        ContractLink = draft.ContractLink;
        CustomerLink = draft.CustomerLink;
        PickupLink = draft.PickupLink;
        ReturnLink = draft.ReturnLink;
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
            PassportLink = PassportLink,
            ContractLink = ContractLink,
            CustomerLink = CustomerLink,
            PickupLink = PickupLink,
            ReturnLink = ReturnLink
        };
    }

    public ReservationReceipt ToReservation(string brand)
    {
        return new ReservationReceipt
        {
            DriverName           = AccountFullName,
            DriverId             = DriverId,
            CarLicense           = CarLicense,
            CarType              = CarType,             // not in response yet
            OriginAddress        = OriginAddress,
            ReservationStartTime = ReportStartDate,
            ReservationEndTime   = ReservationEndDate,
            ReservationCost      = ReservationCost,
            ReservationId        = ReservationNumber,
            DistanceKm           = DistanceKm,
            Brand               = brand
        };
        
    }

    public void ClearFields()
    {
        Url = string.Empty;

        ServiceType = string.Empty;
        ReportStartDate = string.Empty;
        ReportEndDate = string.Empty;
        CarLicense = string.Empty;
        ReservationNumber = string.Empty;
        ReportNumber = string.Empty;
        AccountFullName = string.Empty;
        DriverId = string.Empty;
        DriverLicense = string.Empty;
        Email = string.Empty;
        Phone = string.Empty;
        Address = string.Empty;
        House = string.Empty;
        City = string.Empty;
        PostalCode = string.Empty;
        CreatedOn = string.Empty;
        LicenseLink = string.Empty;
        PassportLink = string.Empty;
        ContractLink = string.Empty;
        CustomerLink = string.Empty;
        PickupLink = string.Empty;
        ReturnLink = string.Empty;

        ReservationEndDate = string.Empty;
        OriginType = string.Empty;
        CarType = string.Empty;
        OriginAddress = string.Empty;
        DistanceKm = string.Empty;
        ReservationCost = 0m;
    }
}