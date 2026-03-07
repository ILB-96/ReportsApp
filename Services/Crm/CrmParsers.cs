using Reports.Utilities;

namespace Reports.Services.Crm;

public sealed record DriverData(
    string Brand,
    string ServiceType,
    string CarLicense,
    string AccountFullName,
    string ReservationNumber,
    string ReportTime,
    bool IsLeasing,
    string ReportNumber,
    string DriverId,
    string DriverLicense,
    string Email,
    string Phone,
    string Address,
    string House,
    string City,
    string PostalCode,
    string CreatedOn,
    string LicenseLink,
    string PassportLink,
    string ReportCity,
    string ReportAddress,
    string ReportPrice,
    string ReportReason,
    string DriverName
);

public static class CrmParsers
{
    public static (string accountId, bool isLeasing, string contactId, DriverData partial) ParseIncident(Dictionary<string, object>? incident, string brand)
    {
        var accountId = incident.GetString("_customerid_value") ?? string.Empty;

        var reservationNumber = incident.GetString("c2g_responsibledriverreservationid") ?? string.Empty;
        var reportTime = incident.GetString("c2g_executiondate@OData.Community.Display.V1.FormattedValue") ?? string.Empty;
        
        var reportAddress = incident.GetString("c2g_reportaddress") ?? string.Empty;
        var reportCity = incident.GetFirstNonEmpty([
            "new_municipality", "_gtg_reportcity_value@OData.Community.Display.V1.FormattedValue"
        ]);
        var reportPrice = incident.GetString("c2g_reportcost@OData.Community.Display.V1.FormattedValue") ?? string.Empty;
        var reportReason = incident.GetString("_c2g_reportreason_value@OData.Community.Display.V1.FormattedValue") ?? string.Empty;
        var driverName =  incident.GetString("_primarycontactid_value@OData.Community.Display.V1.FormattedValue") ?? string.Empty;
        var reportNumber = incident.GetString("c2g_reportnumber") ?? string.Empty;
        var carLicense = incident.GetString("_new_vehicle_value@OData.Community.Display.V1.FormattedValue") ?? string.Empty;
        
        var isLeasingRaw = incident.GetString("gtg_leasingreport");
        var isLeasing = bool.TryParse(isLeasingRaw, out var b) && b;
        var leasingType = incident.GetString("gtg_store@OData.Community.Display.V1.FormattedValue");
        var serviceType = string.IsNullOrEmpty(leasingType) ? brand : leasingType;
        
        var contactId = incident.GetString("_c2g_responsibledriver_value") ?? string.Empty;
        
        var partial = new DriverData(
            Brand: brand,
            ServiceType: serviceType,
            CarLicense: carLicense,
            AccountFullName: "",
            ReservationNumber: reservationNumber,
            ReportTime: reportTime,
            IsLeasing: isLeasing,
            ReportNumber: reportNumber,
            DriverId: "",
            DriverLicense: "",
            Email: "",
            Phone: "",
            Address: "",
            House: "",
            City: "",
            PostalCode: "",
            CreatedOn: "",
            LicenseLink: "",
            PassportLink: "",
            ReportCity: reportCity,
            ReportAddress: reportAddress,
            ReportPrice: reportPrice,
            ReportReason: reportReason,
            DriverName: driverName
        );

        return (accountId, isLeasing, contactId, partial);
    }

    public static DriverData MergeAccount(DriverData baseData, Dictionary<string, object>? account)
    {
        var driverId = account.GetFirstNonEmpty("c2g_idno", "c2g_privatecompanyno").DigitsOnly();
        var driverLicense = account.GetFirstNonEmpty("c2g_licenseno", "c2g_drivinglicenseno").DigitsOnly();
        var fullName = account.GetFirstNonEmpty("fullname", "name", "_c2g_driverid_value@OData.Community.Display.V1.FormattedValue");
        var email = account.GetFirstNonEmpty("emailaddress1", "emailaddress2");
        var phone = account.GetFirstNonEmpty("address2_telephone1", "c2g_pointofcontacttelephone", "address1_telephone2").NormalizePhone();

        var address = account.GetString("address1_line1") ?? string.Empty;
        var house = account.GetFirstNonEmpty("address2_line1", "gtg_housenumber", "address2_composite");
        var city = account.GetString("address1_city") ?? string.Empty;
        var postalCode = account.GetString("address1_postalcode") ?? string.Empty;

        var createdOn = account.GetString("createdon@OData.Community.Display.V1.FormattedValue") ?? string.Empty;

        var licenseLink = account.GetString("c2g_driverlicensefront") ?? string.Empty;
        var passportLink = account.GetString("gtg_passportlink") ?? string.Empty;

        return baseData with
        {
            AccountFullName = fullName,
            DriverId = driverId,
            DriverLicense = driverLicense,
            Email = email,
            Phone = phone,
            Address = address,
            House = house,
            City = city,
            PostalCode = postalCode,
            CreatedOn = createdOn,
            LicenseLink = licenseLink,
            PassportLink = passportLink
        };
    }
}
