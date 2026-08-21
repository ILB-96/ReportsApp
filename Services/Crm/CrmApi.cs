using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Reports.Utilities;

namespace Reports.Services.Crm;

public sealed class CrmApi(HttpClient http) : IDisposable
{
    public Task<Dictionary<string, object>?> GetIncidentAsync(string incidentId, CancellationToken ct = default)
        => GetAsync($"/api/data/v9.0/incidents({incidentId})", "incident.json", ct);

    public Task<Dictionary<string, object>?> GetAccountAsync(string accountId, CancellationToken ct = default)
        => GetAsync($"/api/data/v9.0/accounts({accountId})", "account.json", ct);

    public Task<Dictionary<string, object>?> GetContactAsync(string accountId, CancellationToken ct = default)
        => GetAsync($"/api/data/v9.0/contacts({accountId})", "account.json", ct);

    public Task<Guid?> CreateParkingFineIncidentAsync(ParkingFinePayload payload, CancellationToken ct = default)
        => PostIncidentAsync(BuildParkingFineIncidentPayload(payload), ct);

    public Task<Dictionary<string, object>?> GetVehicleByPlateAsync(string licensePlate, CancellationToken ct = default)
    {
        return GetAsync(
            $"/api/data/v9.0/new_vehicles?fetchXml={Uri.EscapeDataString(BuildVehicleByPlateFetchXml(licensePlate))}",
            "vehicle.json",
            ct);
    }

    private async Task<Dictionary<string, object>?> GetAsync(
        string path, string outputFileName, CancellationToken ct = default)
    {
        using var resp = await http.GetAsync(path, ct).ConfigureAwait(false);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        await SaveResponseJsonPrettyAsync(json, outputFileName, ct).ConfigureAwait(false);

        return JsonSerializer.Deserialize<Dictionary<string, object>>(json);
    }

    public async Task<Guid?> PostIncidentAsync(object payload, CancellationToken ct = default)
    {
        var (_, entityUrl) = await PostAsync("/api/data/v9.0/incidents", payload, ct: ct).ConfigureAwait(false);
        return entityUrl is null ? null : ExtractGuidFromEntityUrl(entityUrl);
    }

    public async Task<(Dictionary<string, object>? Body, Uri? EntityUrl)> PostAsync(
        string path, object payload, string? outputFileName = null, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var resp = await http.PostAsync(path, content, ct).ConfigureAwait(false);
        var respJson = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"CRM POST {path} failed ({(int)resp.StatusCode}): {respJson}");

        if (!string.IsNullOrWhiteSpace(outputFileName))
            await SaveResponseJsonPrettyAsync(respJson, outputFileName, ct).ConfigureAwait(false);

        Uri? entityUrl = null;
        if (resp.Headers.TryGetValues("OData-EntityId", out var values))
        {
            var first = values.FirstOrDefault();
            if (Uri.TryCreate(first, UriKind.Absolute, out var parsed))
                entityUrl = parsed;
        }

        var body = string.IsNullOrWhiteSpace(respJson)
            ? null
            : JsonSerializer.Deserialize<Dictionary<string, object>>(respJson);

        return (body, entityUrl);
    }

    private static Guid? ExtractGuidFromEntityUrl(Uri entityUrl)
    {
        var s = entityUrl.ToString();
        var open = s.LastIndexOf('(');
        var close = s.LastIndexOf(')');
        if (open < 0 || close <= open) return null;

        return Guid.TryParse(s.Substring(open + 1, close - open - 1), out var g) ? g : null;
    }

    private static async Task<string> SaveResponseJsonPrettyAsync(
        string json, string outputFileName, CancellationToken ct = default)
    {
        using var doc = JsonDocument.Parse(json);
        var pretty = JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(outputFileName, pretty, ct).ConfigureAwait(false);
        return outputFileName;
    }

    public async Task<string> ExtractCrmContactId(string urlOrId, CancellationToken ct = default)
    {
        var id = ExtractCrmId(urlOrId);

        if (!urlOrId.Contains("incident")) return "";

        var incident = await GetIncidentAsync(id, ct).ConfigureAwait(false);
        return incident.GetString("_c2g_responsibledriver_value") ?? string.Empty;
    }

    public async Task<string> ExtractCrmAccountId(string urlOrId, CancellationToken ct = default)
    {
        var id = ExtractCrmId(urlOrId);

        if (!urlOrId.Contains("incident")) return id;

        var incident = await GetIncidentAsync(id, ct).ConfigureAwait(false);
        return incident.GetString("_customerid_value") ?? string.Empty;
    }

    public string ExtractCrmId(string urlOrId)
    {
        if (string.IsNullOrWhiteSpace(urlOrId)) return string.Empty;

        if (Uri.TryCreate(urlOrId, UriKind.Absolute, out var uri))
        {
            var q = System.Web.HttpUtility.ParseQueryString(uri.Query);
            var id = q.Get("id");
            if (!string.IsNullOrWhiteSpace(id)) return id;
        }

        return urlOrId.Contains("&id=") ? urlOrId.Split("&id=")[1] : urlOrId;
    }

    public Uri BuildIncidentBrowserUrl(Guid incidentId)
    {
        var baseUri = http.BaseAddress
                      ?? throw new InvalidOperationException("CRM HttpClient has no BaseAddress.");

        var orgRoot = $"{baseUri.Scheme}://{baseUri.Authority}";

        return new Uri($"{orgRoot}/main.aspx?etn=incident&pagetype=entityrecord&id={incidentId}");
    }
    private static string BuildVehicleByPlateFetchXml(string licensePlate)
    {
        var plate = System.Security.SecurityElement.Escape(licensePlate?.Trim() ?? string.Empty);
        return $"""
                <fetch version="1.0" output-format="xml-platform" mapping="logical" distinct="false" savedqueryid="27b1f05e-4453-49f3-8c04-bec82ace8c8a" returntotalrecordcount="true" page="1" count="25" no-lock="false">
                  <entity name="new_vehicle">
                    <attribute name="statecode"/>
                    <attribute name="new_vehicleid"/>
                    <attribute name="new_name"/>
                    <attribute name="createdon"/>
                    <attribute name="statuscode"/>
                    <attribute name="new_parkingaddress"/>
                    <attribute name="ownerid"/>
                    <attribute name="new_noofopentasks"/>
                    <attribute name="modifiedon"/>
                    <attribute name="modifiedby"/>
                    <attribute name="new_fleettype"/>
                    <attribute name="createdby"/>
                    <attribute name="new_businessunit"/>
                    <attribute name="new_assettype"/>
                    <attribute name="new_assetnickname"/>
                    <filter type="and">
                      <condition attribute="statecode" operator="eq" value="0"/>
                      <condition attribute="new_businessunit" operator="eq-businessid"/>
                    </filter>
                    <order attribute="new_name" descending="false"/>
                    <filter type="or" isquickfindfields="1">
                      <condition attribute="new_name" operator="like" value="{plate}%"/>
                    </filter>
                  </entity>
                </fetch>
                """;
    }
    Dictionary<string, object?> BuildParkingFineIncidentPayload(ParkingFinePayload r) => new()
    {
        // --- per-case inputs ---
        ["c2g_commissioncost"]              = 75,
        ["c2g_executiondate"]               = r.ExecutionDate,
        ["c2g_reportaddress"]               = r.ReportAddress,
        ["c2g_reportnumber"]                = r.ReportNumber,
        ["new_municipality"]                = r.Municipality,
        ["description"]                     = r.Description,
        ["c2g_responsibledriverreservationid"] = r.ReservationNumber == "" ? null : r.ReservationNumber,
        ["c2g_ResponsibleCustomer@odata.bind"]      = $"/accounts({r.AccountId})",
        ["c2g_customer_search_account@odata.bind"]  = $"/accounts({r.AccountId})",
        ["customerid_account@odata.bind"]           = $"/accounts({r.AccountId})",
        ["new_CustomerNew@odata.bind"]              = $"/accounts({r.AccountId})",
        ["c2g_ReportReason@odata.bind"]     = r.ReportReason is null ? null : $"/c2g_reportreasons({r.ReportReason})",
        ["gtg_ReportCity@odata.bind"]       = $"/gtg_cities({r.CityId})",
        ["new_Vehicle@odata.bind"]          = $"/new_vehicles({r.VehicleId})",
        ["ownerid@odata.bind"]              = r.ServiceType is "autotel" ? "/systemusers(05aa52f5-23e2-ef11-8eea-7c1e5262002b)" : "/systemusers(3cace5de-1be2-ef11-a731-0022488814a3)",

        // --- baked constants (Israel BU parking-fine flow) ---
        ["c2g_BusinessUnit@odata.bind"]     = r.ServiceType is "autotel" ? "businessunits(22dfba97-ff37-ed11-9db1-0022487fed95)" : "/businessunits(eb8212a1-c820-ea11-a810-000d3a2d5883)",
        ["transactioncurrencyid@odata.bind"]= "/transactioncurrencies(efcf89c9-e053-ea11-a812-000d3a2d5883)",
        ["new_Subject@odata.bind"]          = "/new_subjects(c31f0689-8d47-ec11-8c61-6045bd8d2804)",     // Fines & Authorities
        ["new_SubSubject@odata.bind"]       = "/new_subjects(30200689-8d47-ec11-8c61-6045bd8d2804)",     // Parking fine
        ["subjectid@odata.bind"]            = r.Municipality == "משטרת ישראל" ? "/subjects(203a4e81-4b36-ea11-a813-000d3a27b751)" : "/subjects(c535548d-4b36-ea11-a813-000d3a27b751)",          // Parking Report
        ["title"]                           = "*Choose Subject*",
        ["caseorigincode"]                  = 600920002,
        ["statecode"]                       = 0,
        ["statuscode"]                      = 1,
        ["gtg_priority"]                    = 962940003,
        ["gtg_operationcasestatus"]         = 962940000,
        ["gtg_canreturntoservicecode"]      = 600920000,
        ["gtg_confirmation"]                = 1,
        ["gtg_promotionemail"]              = 962940000,
        ["gtg_reminder"]                    = 962940001,
        ["gtg_store"]                       = r.Store,
        ["gtg_leasingreport"]               = r.ServiceType is "lease" or "colmobil",
        
        // amounts default 0
        ["c2g_amountwechargedourcustomer"]  = 0,
        ["c2g_amountwereceivedfrom3rdpartyourinsurance"] = 0,
        ["c2g_amountwerefundedourcustomer"] = 0,
        ["c2g_costexpense"]                 = 0,
        ["c2g_repaircost3rdpartyvehicle"]   = 0,
        ["c2g_repaircostgotovehicle"]       = 0,
        ["c2g_reportcost"] = r.ReportCost,

        // all the false flags
        ["blockedprofile"] = false, ["c2g_3rdpartydriverslicensephoto"] = false,
        ["c2g_3rdpartypolicyphoto"] = false, ["c2g_accidentplacephoto"] = false,
        ["c2g_accidentreportdocument"] = false, ["c2g_customerawaitingreply"] = false,
        ["c2g_damagephotosofthevehiclesinvolved"] = false, ["c2g_listeningtoacall"] = false,
        ["c2g_rescue"] = false, ["c2g_reservationchangedtoiregular"] = false,
        ["c2g_smartcardactivation"] = false, ["c2g_taxiwasordered"] = false,
        ["c2g_towing"] = false, ["gtg_isownerrelatedtocallcenter"] = false,
        ["gtg_ispolicereport"] = false, ["gtg_movetodamage"] = false,
        ["gtg_ticketonmaintenance"] = false, ["new_licensecheckandupdatelicensetypes"] = false,
        ["new_pincodesetting"] = false, ["new_rateplanchange"] = false,
    };
    public void Dispose() => http.Dispose();
}