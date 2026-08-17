using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Reports.Services.BetterwayApi;

public interface IBetterwayClientSearch
{
    Task<ClientSearchResult> SearchAllProfilesAsync(
        string searchTerm,
        CancellationToken ct = default);
}

public sealed class BetterwayClientSearch(
    HttpClient http,
    IBetterwayTokenProvider tokenProvider) : IBetterwayClientSearch
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static readonly BetterwayProfile[] AllProfiles =
        Enum.GetValues<BetterwayProfile>();

    public async Task<ClientSearchResult> SearchAllProfilesAsync(
        string searchTerm,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            throw new ArgumentException("Search term must not be empty.", nameof(searchTerm));

        Console.WriteLine($"[Betterway] Client SearchAllProfiles start. Term={searchTerm}");

        var token = await tokenProvider.GetBearerTokenAsync(ct);

        var tasks = AllProfiles
            .Select(profile => SearchOneProfileAsync(profile, searchTerm, token, ct))
            .ToArray();

        var perProfileHits = await Task.WhenAll(tasks);

        var allHits = perProfileHits
            .SelectMany(x => x)
            .ToList();

        var profilesWithMatch = allHits
            .Select(h => h.Profile)
            .Distinct()
            .ToList();

        Console.WriteLine($"[Betterway] Client SearchAllProfiles done. Hits={allHits.Count}, Profiles=[{string.Join(",", profilesWithMatch)}]");

        return new ClientSearchResult(
            FirstMatch: allHits.Count > 0 ? allHits[0].Client : null,
            ProfilesWithMatch: profilesWithMatch,
            AllHits: allHits);
    }

    private async Task<IReadOnlyList<ClientSearchHit>> SearchOneProfileAsync(
        BetterwayProfile profile,
        string searchTerm,
        string token,
        CancellationToken ct)
    {
        var body = new ClientSearchRequest(searchTerm);

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/LeasingCompanies/Clients/Search");
        request.Content = JsonContent.Create(body, options: JsonOptions);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Profile-Id", ((int)profile).ToString(CultureInfo.InvariantCulture));

        try
        {
            using var response = await http.SendAsync(request, ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                Console.WriteLine($"[Betterway] Client search failed for profile {profile}: {(int)response.StatusCode}. Body: {errorBody}");
                return Array.Empty<ClientSearchHit>();
            }

            var parsed = await response.Content.ReadFromJsonAsync<ClientSearchResponse>(JsonOptions, ct);
            var items = parsed?.Items ?? new List<BetterwayClient>();

            Console.WriteLine($"[Betterway] Client search profile {profile}: {items.Count} hit(s)");

            return items
                .Select(c => new ClientSearchHit(profile, c))
                .ToList();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Console.WriteLine($"[Betterway] Client search threw for profile {profile}: {ex.Message}");
            return Array.Empty<ClientSearchHit>();
        }
    }
}