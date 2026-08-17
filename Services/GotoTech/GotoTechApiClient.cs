using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Reports.Services.GotoTech;

public sealed class GotoTechApiClient(HttpClient httpClient, IGotoTechTokenProvider tokenProvider)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // ── typed methods ────────────────────────────────────────────────────────

    public Task<GotoTechResponse> GetReservationAsync(
        BoRegion region, string reservationId, CancellationToken ct = default) =>
        SendAsync(region, "GetReservation", $"/{reservationId}", ct: ct);

    public Task<GotoTechResponse> GetCarBoAsync(
        BoRegion region, long? carId, CancellationToken ct = default) =>
        SendAsync(region, "GetCarBO", $"/{carId}", ct: ct);

    // ── general sender ───────────────────────────────────────────────────────

    public async Task<GotoTechResponse> SendAsync(
        BoRegion region,
        string opcode,
        string? data,
        HttpMethod? method = null,
        CancellationToken ct = default)
    {
        var token   = tokenProvider.GetToken(region);
        var origin  = BoEndpoints.GetOrigin(region);
        var apiUrl  = BoEndpoints.GetApiUrl(region);

        // Username and Password are literal "x" — auth is entirely via x-token header
        var body = new
        {
            Username = "x",
            Password = "x",
            Opcode   = opcode,
            Data     = data
        };

        using var content = new StringContent(
            JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, apiUrl)
        {
            Content = content
        };

        request.Headers.Add("x-token",        token);
        request.Headers.Add("x-goto-device",  "BO");
        request.Headers.Add("Origin",          origin);
        request.Headers.Add("Referer",         origin + "/");
        request.Headers.Add("Accept",          "application/json, text/plain, */*");

        using var response = await httpClient.SendAsync(request, ct);
        var text = await response.Content.ReadAsStringAsync(ct);

        if (text.TrimStart().StartsWith('<'))
            throw new InvalidOperationException(
                $"GotoTech BO [{region}] returned HTML instead of JSON " +
                $"(HTTP {(int)response.StatusCode}). Raw response:\n{text[..Math.Min(500, text.Length)]}");

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"GotoTech BO [{region}] returned {(int)response.StatusCode} {response.ReasonPhrase}:\n{text}");

        return JsonSerializer.Deserialize<GotoTechResponse>(text, JsonOptions)
               ?? throw new InvalidOperationException($"GotoTech BO [{region}] returned an empty body.");
    }
}