using Reports.Services.Crm;
using Reports.Services.Drivers;
using Reports.Tabs.CreateAgreement;

namespace Reports.Services.Agreement;

public interface IAgreementDraftService
{
    Task<DriverAgreementData> LoadDraftAsync(CreateAgreementView.AgreementRequestData request, CancellationToken ct = default);
}

public sealed class AgreementDraftService(
    ICrmBrandResolver brandResolver,
    ICrmCookieProvider cookieProvider)
    : IAgreementDraftService
{
    public async Task<DriverAgreementData> LoadDraftAsync(CreateAgreementView.AgreementRequestData request, CancellationToken ct = default)
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
        var accountId = await crm.ExtractCrmAccountId(request.Url);
        var contactId = await crm.ExtractCrmContactId(request.Url);

        if (string.IsNullOrWhiteSpace(accountId) && string.IsNullOrWhiteSpace(contactId))
            return null;

        var account = string.IsNullOrWhiteSpace(contactId)
            ? await crm.GetAccountAsync(accountId)
            : await crm.GetContactAsync(contactId);

        return CrmParsers.ParseAgreement(account);
    }
}