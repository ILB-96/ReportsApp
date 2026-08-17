using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Reports.Services.BetterwayApi;

public interface IBetterwayVehicleSearch
{
    Task<VehicleLookupResult?> FindByPlateAsync(BetterwayProfile profileId, string plate, CancellationToken ct = default);
}

public sealed class BetterwayVehicleSearch(
    HttpClient http,
    IBetterwayTokenProvider tokenProvider) : IBetterwayVehicleSearch
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public async Task<VehicleLookupResult?> FindByPlateAsync(BetterwayProfile profile, string plate, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(plate))
            throw new ArgumentException("Plate must not be empty.", nameof(plate));

        Debug.WriteLine($"[Betterway] Vehicle search start. Plate={plate}");

        var token = await tokenProvider.GetBearerTokenAsync(ct);
        var body = new VehicleSearchRequest(plate);

        // GET with a body (matches the captured request). If the server rejects it, switch to HttpMethod.Post.
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/Vehicles/Mine");
        request.Content = JsonContent.Create(body, options: JsonOptions);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Profile-Id", ((int)profile).ToString(CultureInfo.InvariantCulture));

        try
        {
            using var response = await http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync(ct);
                Debug.WriteLine($"[Betterway] Vehicle search HTTP {(int)response.StatusCode} for plate {plate}: {err}");
                throw new InvalidOperationException($"line 49 Betterway vehicle search failed ({(int)response.StatusCode}).");
            }

            var parsed = await response.Content.ReadFromJsonAsync<VehicleSearchResponse>(JsonOptions, ct);
            var item = parsed?.Items is { Count: > 0 } items ? items[0] : null;
            if (item is null)
            {
                Debug.WriteLine("[Betterway] Vehicle search: no hits.");
                throw new InvalidOperationException($"line 57 No Items found for plate {plate} ({(int)response.StatusCode}).");
            }

            var hasContract = item.ContractProfile is not null;

            return new VehicleLookupResult(
                Id:                 item.Id,
                PlateNumber:        item.PlateNumber ?? plate,
                HasContract:        hasContract,
                ContractStartDate:  hasContract ? Norm(item.StartDate) : null,
                ContractEndDate:    hasContract ? Norm(item.EndDate)   : null,
                OwnershipStartDate: Norm(item.Owner?.StartDate),
                OwnershipEndDate:   Norm(item.Owner?.EndDate));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Debug.WriteLine($"[Betterway] Vehicle search threw: {ex.Message}");
            throw new InvalidOperationException($"line 74 Betterway vehicle search failed ({ex.Message}).");
        }
    }

    private static DateTime? Norm(DateTime? d) =>
        d is null || d.Value == DateTime.MinValue ? null : d;
}