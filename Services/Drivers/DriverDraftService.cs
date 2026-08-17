using System.Text.Json;
using Reports.Services.Crm;
using Reports.Services.GotoTech;
using Reports.Services.Reservation;

namespace Reports.Services.Drivers;

public interface IDriverDraftService
{
    Task<(CreateDriverDraft, ReservationReceipt?)> LoadDraftAsync(CreateDriverRequest request, CancellationToken ct = default);
}
public sealed class DriverDraftService(
    ICrmBrandResolver brandResolver,
    ICrmCookieProvider cookieProvider,
    GotoTechApiClient gotoTechApiClient)
    : IDriverDraftService
{
    public async Task<(CreateDriverDraft, ReservationReceipt?)> LoadDraftAsync(CreateDriverRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
            throw new InvalidOperationException("URL is required.");

        var brand = brandResolver.ServiceTypeFromUrl(request.Url);
        if (string.IsNullOrWhiteSpace(brand))
            throw new InvalidOperationException("Could not determine service type from URL.");

        var cookies = cookieProvider.GetCookiesForUrl(request.Url);
        
        if (cookies.Count == 0)
            throw new InvalidOperationException("No CRM cookies were found for this URL. Open the CRM tab in Chrome and try again.");

        var baseUri = brandResolver.BaseUri(brand);

        using var crm = new CrmApi(CrmClientFactory.Create(baseUri, cookies));

        var incidentId = crm.ExtractCrmId(request.Url.Trim());
        var incident = await crm.GetIncidentAsync(incidentId);
        var (accountId, isLeasing, contactId, partial) = CrmParsers.ParseIncident(incident, brand);

        if (string.IsNullOrWhiteSpace(accountId))
            throw new InvalidOperationException("Account Link is missing.");

        var account = (!isLeasing && !string.IsNullOrWhiteSpace(contactId))
            ? await crm.GetContactAsync(contactId)
            : await crm.GetAccountAsync(accountId);

        var data = CrmParsers.MergeAccount(partial, account);
        ReservationReceipt? reservationReceipt = null;
        if (!string.IsNullOrWhiteSpace(data.ReservationNumber))
        {
            var region = ResolveRegion(brand);
            
            if (region is not null)
            {
                var resp = await gotoTechApiClient.GetReservationAsync(region.Value, data.ReservationNumber, ct);
                if (resp.IsSuccess)
                {
                    var reservation = resp.DeserializeData<JsonElement>();

                    var carId            = reservation.TryGetProperty("carId",          out var p0) ? p0.GetInt64()       : (long?)null;
                    var actualStartDate  = reservation.TryGetProperty("actualStartDate",out var p1) ? ToStringValue(p1)   : string.Empty;
                    var actualEndDate    = reservation.TryGetProperty("actualEndDate",  out var p2) ? ToStringValue(p2)   : string.Empty;
                    var reservationCost  = reservation.TryGetProperty("cost",           out var p3) ? p3.GetDecimal()     : 0;
                    var parkingAddress   = reservation.TryGetProperty("endAddressHe", out var p4) ? ToStringValue(p4)   : string.Empty;
                    var distanceKm       = reservation.TryGetProperty("distanceKM",     out var p5) ? ToStringValue(p5)   : string.Empty;
                    var resp2 = await gotoTechApiClient.GetCarBoAsync(region.Value, carId, ct);
                    var carBo = resp2.DeserializeData<JsonElement>();
                    var carManufacturerName       = carBo.TryGetProperty("carManufacturerName",     out var p6) ? ToStringValue(p6)   : string.Empty;
                    var carModelName       = carBo.TryGetProperty("carModelName",     out var p7) ? ToStringValue(p7)   : string.Empty;
                    if (String.IsNullOrWhiteSpace(parkingAddress))
                        parkingAddress   = reservation.TryGetProperty("startAddressHe", out var p8) ? ToStringValue(p8)   : string.Empty;
                    if (String.IsNullOrWhiteSpace(parkingAddress))
                        parkingAddress   = reservation.TryGetProperty("parkingAddress", out var p9) ? ToStringValue(p9)   : string.Empty;

                    
                    reservationReceipt = new ReservationReceipt
                    {
                        DriverName           = data.AccountFullName,
                        DriverId             = data.DriverId,
                        CarLicense           = data.CarLicense,
                        CarType              = $"{carManufacturerName} {carModelName}",             // not in response yet
                        OriginAddress        = parkingAddress,
                        ReservationStartTime = actualStartDate,
                        ReservationEndTime   = actualEndDate,
                        ReservationCost      = reservationCost,
                        ReservationId        = data.ReservationNumber,
                        DistanceKm           = distanceKm,
                        Brand               = brand
                    };
                }
            }
        }

        string? ToStringValue(JsonElement el) => el.ValueKind switch
        {
            JsonValueKind.String  => el.GetString(),
            JsonValueKind.Number  => el.GetRawText(),
            JsonValueKind.True    => "true",
            JsonValueKind.False   => "false",
            JsonValueKind.Null    => null,
            _                     => el.GetRawText()
        };
        var driverDraft = new CreateDriverDraft
        {
            Brand = brand,
            ServiceType = NormalizeServiceType(data.ServiceType),
            ReportStartDate = data.ReportTime,
            ReportEndDate = data.ReportTime,
            CarLicense = data.CarLicense,
            ReservationNumber = data.ReservationNumber,
            ReportNumber = data.ReportNumber,
            AccountFullName = data.AccountFullName,
            DriverId = data.DriverId,
            DriverLicense = data.DriverLicense,
            Email = data.Email,
            Phone = data.Phone,
            Address = data.Address,
            House = data.House,
            City = data.City,
            PostalCode = data.PostalCode,
            CreatedOn = data.CreatedOn,
            LicenseLink = data.LicenseLink,
            PassportLink = data.PassportLink,
            ContractLink = data.ContractLink,
            CustomerLink = data.CustomerLink,
            PickupLink = data.PickupLink,
            ReturnLink = data.ReturnLink
        };
        return (driverDraft, reservationReceipt);
    }
    private static BoRegion? ResolveRegion(string brand)
    {
        var lower = brand.ToLowerInvariant();

        if (lower.Contains("goto"))
            return BoRegion.Car2Go;

        if (lower.Contains("autotel"))
            return BoRegion.Autotel;

        return null;
    }


    private static string NormalizeServiceType(string brand)
    {
        if (brand.Contains("Lease"))
            return "lease";

        if (brand.Contains("lease"))
            return "colmobil";

        return brand;
    }
}