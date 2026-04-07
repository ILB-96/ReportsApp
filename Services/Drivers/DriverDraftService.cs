using Reports.Services.Crm;

namespace Reports.Services.Drivers;

public interface IDriverDraftService
{
    Task<CreateDriverDraft> LoadDraftAsync(CreateDriverRequest request, CancellationToken ct = default);
}
public sealed class DriverDraftService(
    ICrmBrandResolver brandResolver,
    ICrmCookieProvider cookieProvider)
    : IDriverDraftService
{
    public async Task<CreateDriverDraft> LoadDraftAsync(CreateDriverRequest request, CancellationToken ct = default)
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

        return new CreateDriverDraft
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
            PassportLink = data.PassportLink
        };
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