using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Reports.Services.BetterwayApi;

public interface IBetterwayDriverApi
{
    Task<string> CreateDriverAsync(DriverImportPayload payload,
        BetterwayProfile profile,
        CancellationToken ct = default);
}

public sealed record DriverImportPayload(
    string PlateNumber,
    DateTime ContractStartDate,
    DateTime ContractEndDate,
    string Name,
    string IdNumber,
    string PhoneNumber,
    string LicenseNumber,
    string Email,
    string Street,
    string HouseNumber,
    string City,
    string ZipCode);

public sealed class BetterwayNewDriver(
    HttpClient http,
    IBetterwayTokenProvider tokenProvider) : IBetterwayDriverApi
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public async Task<string> CreateDriverAsync(DriverImportPayload payload,
        BetterwayProfile profile,
        CancellationToken ct = default)
    {
        Debug.WriteLine($"[Betterway] ImportDriver start. Profile={profile} ({(int)profile}), Plate={payload.PlateNumber}, Id={payload.IdNumber}");

        var token = await tokenProvider.GetBearerTokenAsync(ct).ConfigureAwait(false);
        Debug.WriteLine($"[Betterway] Got bearer token (length={token.Length}, prefix={token[..Math.Min(12, token.Length)]}...)");

        // Serialize once so we can both log it and send it. JsonContent.Create defers
        // serialization until the request is sent, which is fine — but for debugging
        // it's much easier to see the exact bytes going on the wire.
        var json = JsonSerializer.Serialize(payload, JsonOptions);
        Debug.WriteLine($"[Betterway] Request body: {json}");

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/Drivers/Import")
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Profile-Id", ((int)profile).ToString(CultureInfo.InvariantCulture));

        Debug.WriteLine($"[Betterway] Sending POST {http.BaseAddress}api/Drivers/Import");

        using var response = await http.SendAsync(request, ct).ConfigureAwait(false);
        var responseBody = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

        Debug.WriteLine($"[Betterway] Response: {(int)response.StatusCode} {response.ReasonPhrase}");
        Debug.WriteLine($"[Betterway] Response body: {responseBody}");

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"Driver import failed: {(int)response.StatusCode} {response.ReasonPhrase}. Body: {responseBody}");
        }

        return responseBody;
    }
}