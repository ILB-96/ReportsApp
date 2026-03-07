using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Reports.Services.Crm;

public sealed class CrmApi(HttpClient http) : IDisposable
{
    public Task<Dictionary<string, object>?> GetIncidentAsync(string incidentId)
        => GetAsync($"/api/data/v9.0/incidents({incidentId})", "incident.json");

    public Task<Dictionary<string, object>?> GetAccountAsync(string accountId)
        => GetAsync($"/api/data/v9.0/accounts({accountId})", "account.json");
    
    public Task<Dictionary<string, object>?> GetContactAsync(string accountId)
        => GetAsync($"/api/data/v9.0/contacts({accountId})", "account.json");

    private async Task<Dictionary<string, object>?> GetAsync(string path, string outputFileName)
    {
        using var resp = await http.GetAsync(path);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadAsStringAsync();
        
        await SaveResponseJsonPrettyAsync(json, outputFileName);
        
        return JsonSerializer.Deserialize<Dictionary<string, object>>(json);
    }
    public Task<Dictionary<string, object>?> PostIncidentAsync(object payload)
        => PostAsync($"/api/data/v9.0/incidents", payload, "incident.json");
    
    public async Task<Dictionary<string, object>?> PostAsync(string path, object payload, string? outputFileName = null)
    {
        var json = JsonSerializer.Serialize(payload);

        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var resp = await http.PostAsync(path, content);

        resp.EnsureSuccessStatusCode();

        var respJson = await resp.Content.ReadAsStringAsync();

        if (!string.IsNullOrWhiteSpace(outputFileName))
            await SaveResponseJsonPrettyAsync(respJson, outputFileName);

        return string.IsNullOrWhiteSpace(respJson)
            ? null
            : JsonSerializer.Deserialize<Dictionary<string, object>>(respJson);
    }
    private async Task<string> SaveResponseJsonPrettyAsync(string json, string outputFileName)
    {

        using var doc = JsonDocument.Parse(json);
        var pretty = JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(outputFileName, pretty);
        return outputFileName;
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
    
    public void Dispose() => http.Dispose();
}