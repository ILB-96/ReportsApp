using System.Globalization;
using System.Text;
using System.Text.Json;
using Reports.Services.Crm;
using Reports.Tabs.CreateIncident;

namespace Reports.Services.Incident;

public interface IIncidentDraftService
{
    Task<ParkingFinePayload> LoadDraftAsync(CreateIncidentView.IncidentRequestData request, CancellationToken ct = default);
}

public sealed class IncidentDraftService(
    ICrmBrandResolver brandResolver,
    ICrmCookieProvider cookieProvider)
    : IIncidentDraftService
{
    private static readonly string[] KnownLabels =
    [
        "מספר דוח", "מספר רכב", "עיריה", "זמן קבלת הדוח",
        "סטטוס", "שם לקוח", "אסמכתה", "סכום מקורי", "סכום ששולם",
        "בעל הרכב", "היטל עובד", "יתרה לתשלום", "תאריך יצירה",
        "תאריך אחרון לתשלום", "כתובת", "תיאור המקרה", "תקנה",
        "מזהה עיריה", "תאריך אימות"
    ];

    public async Task<ParkingFinePayload> LoadDraftAsync(CreateIncidentView.IncidentRequestData request,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
            throw new InvalidOperationException("URL is required.");

        var brand = brandResolver.ServiceTypeFromUrl(request.Url);
        if (string.IsNullOrWhiteSpace(brand))
            throw new InvalidOperationException("Could not determine service type from URL.");
        var fineData = ExtractFineData(request.Data);
        
        var cookies = cookieProvider.GetCookiesForUrl(request.Url);
        
        if (cookies.Count == 0)
            throw new InvalidOperationException(
                "No CRM cookies were found for this URL. Open the CRM tab in Chrome and try again.");
        
        var baseUri = brandResolver.BaseUri(brand);
        
        using var crm = new CrmApi(CrmClientFactory.Create(baseUri, cookies));
        var digits = new string(fineData.VehiclePlateNumber.Where(char.IsDigit).ToArray());
        string formatted = digits.Length switch
        {
            8 => $"{digits[..3]}-{digits[3..5]}-{digits[5..]}",
            7 => $"{digits[..2]}-{digits[2..5]}-{digits[5..]}",   // 2-3-2 for 7-digit plates
            _ => digits
        };
        var vehicle = await crm.GetVehicleByPlateAsync(formatted);
        if (vehicle is null || !vehicle.TryGetValue("value", out var v) || v is not JsonElement value)
            return null;

        if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() == 0)
            return null;   // plate not found

        var s = value[0].TryGetProperty("new_vehicleid", out var id)
            ? id.GetString()
            : null;
        var accountId = await crm.ExtractCrmAccountId(request.Url);
        
        fineData = fineData with
        {
            AccountId = accountId,
            VehicleId = s,
            VehiclePlateNumber = formatted
        };

        return fineData;
    }

    private static ParkingFinePayload ExtractFineData(string data)
    {


    var f = ParseFields(data);

    string Get(string k) => f.GetValueOrDefault(k, "");

    string? GetOpt(string k) => f.TryGetValue(k, out var v) && !string.IsNullOrWhiteSpace(v) ? v : null;

    return new ParkingFinePayload(
        AccountId:           null,                       // not in this text
        VehicleId:           null,                       // not in this text
        VehiclePlateNumber:  Get("מספר רכב"),
        ReportCost:          int.TryParse(GetOpt("סכום מקורי"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : 0,        // original amount (see note)
        ReportReason:        GetOpt("תיאור המקרה"),
        Municipality:        Get("עיריה"),
        ReportAddress:       Get("כתובת"),
        ReportNumber:        Get("מספר דוח"),
        ExecutionDate:       ToUtcIso(Get("זמן קבלת הדוח")),
        ReservationNumber:   null,
        Description:         null,
        CityId:              null,
        Store:               null,
        ServiceType:         null);
    }
    private static Dictionary<string, string> ParseFields(string data)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);

        var lines = data.Replace("\r", "")
                        .Split('\n')
                        .Select(l => l.Trim())
                        .Where(l => l.Length > 0)
                        .ToList();

        bool IsLabel(string line, out string label, out string inline)
        {
            foreach (var lbl in KnownLabels)
            {
                if (line.StartsWith(lbl, StringComparison.Ordinal))
                {
                    label = lbl;
                    var rest = line[lbl.Length..].TrimStart();
                    inline = rest.StartsWith(':') ? rest[1..].Trim() : rest; // ":val" or " val" or ""
                    return true;
                }
            }
            label = ""; inline = ""; return false;
        }

        for (int i = 0; i < lines.Count; i++)
        {
            if (!IsLabel(lines[i], out var label, out var inline))
                continue; // titles / section headers ("דוח תעבורה", "פרטי דוח")

            if (label == "אסמכתה")           // label carries "(ירושלים)" — not a value
                inline = "";

            if (inline.Length > 0)            // inline value (with ':' or the space-separated datetime)
            {
                result[label] = inline;
                continue;
            }

            // value lives on the following line(s), up to the next known label
            var sb = new StringBuilder();
            int j = i + 1;
            while (j < lines.Count && !IsLabel(lines[j], out _, out _))
            {
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(lines[j]);
                j++;
            }
            result[label] = sb.ToString().Trim();
            i = j - 1;
        }

        return result;
    }

    private static string ToUtcIso(string raw)
    {
        Console.WriteLine(raw);
        if (string.IsNullOrWhiteSpace(raw)) return "";
        string[] formats = { "dd/MM/yyyy HH:mm", "dd/MM/yyyy" };
        if (!DateTime.TryParseExact(raw.Trim(), formats, CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var local))
            return "";

        var utc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(local, DateTimeKind.Unspecified), GetIsraelTz());
        return utc.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
    }

    private static TimeZoneInfo GetIsraelTz()
    {
        foreach (var id in new[] { "Israel Standard Time", "Asia/Jerusalem" })
            try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        return TimeZoneInfo.Utc;
    }
}