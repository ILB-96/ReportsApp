using System.Diagnostics;
using Reports.Services.Crm;
using Reports.Tabs.CreateIncident;


namespace Reports.Services.Incident;

public interface IIncidentSubmissionService
{
    Task<string?> SubmitAsync(ParkingFinePayload submission, CreateIncidentView.IncidentRequestData request, CancellationToken ct = default);
}
public sealed class IncidentSubmissionService(ICrmBrandResolver brandResolver,
    ICrmCookieProvider cookieProvider)
    : IIncidentSubmissionService
{
    public async Task<string?> SubmitAsync(ParkingFinePayload submission,CreateIncidentView.IncidentRequestData request, CancellationToken ct = default)
    {
        var brand = brandResolver.ServiceTypeFromUrl(request.Url);
        var cookies = cookieProvider.GetCookiesForUrl(request.Url);
        
        if (cookies.Count == 0)
            throw new InvalidOperationException(
                "No CRM cookies were found for this URL. Open the CRM tab in Chrome and try again.");
        
        var baseUri = brandResolver.BaseUri(brand);
        
        using var crm = new CrmApi(CrmClientFactory.Create(baseUri, cookies));

        var incidentId = await crm.CreateParkingFineIncidentAsync(submission);
        if (incidentId is null)
        {
            // OData-EntityId header was missing — log and bail.
            return null;
        }
        var url = crm.BuildIncidentBrowserUrl(incidentId.Value);
        OpenInDefaultBrowser(url.ToString());
        return null;
    }
    private void OpenInDefaultBrowser(string url)
        => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

}