using System.Text.Json;
using Reports.Services.Crm;
using Reports.Services.GotoTech;
using Reports.Services.Reservation;

namespace Reports.Services.Drivers;

public interface IDriverDraftService
{
    Task<(CreateDriverDraft, ReservationReceipt?)> LoadDraftAsync(
        CreateDriverRequest request, CancellationToken ct = default);
}

public sealed class DriverDraftService(
    ICrmBrandResolver brandResolver,
    ICrmCookieProvider cookieProvider,
    GotoTechApiClient gotoTechApiClient)
    : IDriverDraftService
{
    public async Task<(CreateDriverDraft, ReservationReceipt?)> LoadDraftAsync(
        CreateDriverRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
            throw new InvalidOperationException("URL is required.");

        var brand = brandResolver.ServiceTypeFromUrl(request.Url);
        if (string.IsNullOrWhiteSpace(brand))
            throw new InvalidOperationException("Could not determine service type from URL.");

        var cookies = cookieProvider.GetCookiesForUrl(request.Url);

        if (cookies.Count == 0)
            throw new InvalidOperationException(
                "No CRM cookies were found for this URL. Open the CRM tab in Chrome and try again.");

        var baseUri = brandResolver.BaseUri(brand);

        using var crm = new CrmApi(CrmClientFactory.Create(baseUri, cookies));

        var incidentId = crm.ExtractCrmId(request.Url.Trim());
        var incident = await crm.GetIncidentAsync(incidentId, ct);
        var (accountId, isLeasing, contactId, partial) = CrmParsers.ParseIncident(incident, brand);

        if (string.IsNullOrWhiteSpace(accountId))
            throw new InvalidOperationException("Account Link is missing.");

        // The reservation number comes from the incident, so the GoTo Tech lookup
        // doesn't need to wait for the CRM account/contact fetch. Start both.
        var accountTask = (!isLeasing && !string.IsNullOrWhiteSpace(contactId))
            ? crm.GetContactAsync(contactId, ct)
            : crm.GetAccountAsync(accountId, ct);

        var receiptTask = LoadReservationReceiptAsync(brand, partial.ReservationNumber, ct);

        await Task.WhenAll(accountTask, receiptTask);

        var account = await accountTask;
        var reservationReceipt = await receiptTask;

        var data = CrmParsers.MergeAccount(partial, account);

        // The receipt is built from GoTo Tech data plus a few CRM fields that
        // are only final after the merge, so fill those in now.
        if (reservationReceipt is not null)
        {
            reservationReceipt = reservationReceipt with
            {
                DriverName = data.AccountFullName,
                DriverId   = data.DriverId,
                CarLicense = data.CarLicense
            };
        }

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
            ContractLink = !string.IsNullOrWhiteSpace(data.ReservationNumber) ? "" : data.ContractLink,
            CustomerLink = !string.IsNullOrWhiteSpace(data.ReservationNumber) ? "" : data.CustomerLink,
            PickupLink = !string.IsNullOrWhiteSpace(data.ReservationNumber) ? "" : data.PickupLink,
            ReturnLink = !string.IsNullOrWhiteSpace(data.ReservationNumber) ? "" : data.ReturnLink
        };

        return (driverDraft, reservationReceipt);
    }

    private async Task<ReservationReceipt?> LoadReservationReceiptAsync(
        string brand, string? reservationNumber, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(reservationNumber))
            return null;

        var region = ResolveRegion(brand);
        if (region is null)
            return null;

        var resp = await gotoTechApiClient.GetReservationAsync(region.Value, reservationNumber, ct);
        if (!resp.IsSuccess)
            return null;

        var reservation = resp.DeserializeData<JsonElement>();

        var carId           = reservation.TryGetProperty("carId",           out var p0) ? p0.GetInt64()     : (long?)null;
        var actualStartDate = reservation.TryGetProperty("actualStartDate", out var p1) ? ToStringValue(p1) : string.Empty;
        var actualEndDate   = reservation.TryGetProperty("actualEndDate",   out var p2) ? ToStringValue(p2) : string.Empty;
        var reservationCost = reservation.TryGetProperty("cost",            out var p3) ? p3.GetDecimal()   : 0;
        var parkingAddress  = reservation.TryGetProperty("endAddressHe",    out var p4) ? ToStringValue(p4) : string.Empty;
        var distanceKm      = reservation.TryGetProperty("distanceKM",      out var p5) ? ToStringValue(p5) : string.Empty;

        if (string.IsNullOrWhiteSpace(parkingAddress))
            parkingAddress = reservation.TryGetProperty("startAddressHe", out var p6) ? ToStringValue(p6) : string.Empty;
        if (string.IsNullOrWhiteSpace(parkingAddress))
            parkingAddress = reservation.TryGetProperty("parkingAddress", out var p7) ? ToStringValue(p7) : string.Empty;

        // Car details are a nice-to-have — a failure here shouldn't lose the receipt.
        var carType = string.Empty;
        if (carId is not null)
        {
            var carResp = await gotoTechApiClient.GetCarBoAsync(region.Value, carId, ct);
            if (carResp.IsSuccess)
            {
                var carBo = carResp.DeserializeData<JsonElement>();
                var manufacturer = carBo.TryGetProperty("carManufacturerName", out var p8) ? ToStringValue(p8) : string.Empty;
                var model        = carBo.TryGetProperty("carModelName",        out var p9) ? ToStringValue(p9) : string.Empty;
                carType = $"{manufacturer} {model}".Trim();
            }
        }

        return new ReservationReceipt
        {
            CarType              = carType,
            OriginAddress        = parkingAddress,
            ReservationStartTime = actualStartDate,
            ReservationEndTime   = actualEndDate,
            ReservationCost      = reservationCost,
            ReservationId        = reservationNumber,
            DistanceKm           = distanceKm,
            Brand                = brand
            // DriverName / DriverId / CarLicense are filled by the caller after the CRM merge.
        };
    }

    private static string? ToStringValue(JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.String => el.GetString(),
        JsonValueKind.Number => el.GetRawText(),
        JsonValueKind.True   => "true",
        JsonValueKind.False  => "false",
        JsonValueKind.Null   => null,
        _                    => el.GetRawText()
    };

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

        if (brand.Contains("Flex"))
            return "colmobil";

        return brand;
    }
}