using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Reports.Services.BetterwayApi;
public interface IBetterwayDriverSearch
{
    Task<DriverSearchResult> SearchAllProfilesAsync(
        string searchTerm,
        CancellationToken ct = default);
}
public sealed class BetterwayDriverSearch(
    HttpClient http,
    IBetterwayTokenProvider tokenProvider) : IBetterwayDriverSearch
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    // Single source of truth for "which profiles do we search". If a 5th profile
    // gets added later, this is the only line that changes.
    private static readonly BetterwayProfile[] AllProfiles =
        Enum.GetValues<BetterwayProfile>();

    public async Task<DriverSearchResult> SearchAllProfilesAsync(
        string searchTerm,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            throw new ArgumentException("Search term must not be empty.", nameof(searchTerm));

        Debug.WriteLine($"[Betterway] SearchAllProfiles start. Term={searchTerm}");

        var token = await tokenProvider.GetBearerTokenAsync(ct);

        // Fire one request per profile in parallel. Same token works for all of them
        // — the profile is conveyed by the Profile-Id header, not by the auth.
        var tasks = AllProfiles
            .Select(profile => SearchOneProfileAsync(profile, searchTerm, token, ct))
            .ToArray();

        var perProfileHits = await Task.WhenAll(tasks);

        // Flatten: each profile may have returned multiple items. We keep all of them
        // tagged with the profile they came from.
        var allHits = perProfileHits
            .SelectMany(x => x)
            .ToList();

        var profilesWithMatch = allHits
            .Select(h => h.Profile)
            .Distinct()
            .ToList();

        Debug.WriteLine($"[Betterway] SearchAllProfiles done. Hits={allHits.Count}, Profiles=[{string.Join(",", profilesWithMatch)}]");

        return new DriverSearchResult(
            FirstMatch: allHits.Count > 0 ? allHits[0].Driver : null,
            ProfilesWithMatch: profilesWithMatch,
            AllHits: allHits);
    }

    private async Task<IReadOnlyList<DriverSearchHit>> SearchOneProfileAsync(
        BetterwayProfile profile,
        string searchTerm,
        string token,
        CancellationToken ct)
    {
        var body = new DriverSearchRequest(searchTerm);

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/Drivers/search");
        request.Content = JsonContent.Create(body, options: JsonOptions);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("Profile-Id", ((int)profile).ToString(CultureInfo.InvariantCulture));

        try
        {
            using var response = await http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(ct);
                Debug.WriteLine($"[Betterway] Search failed for profile {profile}: {(int)response.StatusCode}. Body: {errorBody}");
                // Don't throw — one profile failing shouldn't kill the whole search.
                return Array.Empty<DriverSearchHit>();
            }

            var parsed = await response.Content.ReadFromJsonAsync<DriverSearchResponse>(JsonOptions, ct);
            var items = parsed?.Items ?? new List<BetterwayDriver>();

            Debug.WriteLine($"[Betterway] Search profile {profile}: {items.Count} hit(s)");

            return items
                .Select(c => new DriverSearchHit(profile, c))
                .ToList();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Debug.WriteLine($"[Betterway] Search threw for profile {profile}: {ex.Message}");
            return Array.Empty<DriverSearchHit>();
        }
    }
}