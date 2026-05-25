using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Reports.Services.ChromeSync;
namespace Reports.Services.BetterwayApi;


public interface IBetterwayTokenProvider
{
    Task<string> GetBearerTokenAsync(CancellationToken ct = default);
}

public sealed record BetterwayTokenResponse(
    [property: JsonPropertyName("access_token")]  string AccessToken,
    [property: JsonPropertyName("token_type")]    string TokenType,
    [property: JsonPropertyName("expires_in")]    int    ExpiresIn,
    [property: JsonPropertyName("refresh_token")] string? RefreshToken);

public sealed class BetterwayTokenProvider(
    HttpClient http,
    ChromeSyncStore store) : IBetterwayTokenProvider
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? _accessToken;
    private DateTime _accessTokenExpiresUtc = DateTime.MinValue;

    // Tracks the refresh token we most recently used, so when the server rotates it
    // we can detect that and update the store.
    private string? _lastRefreshTokenUsed;
    private string? _currentRefreshToken;

    public async Task<string> GetBearerTokenAsync(CancellationToken ct = default)
    {
        if (_accessToken is not null && DateTime.UtcNow < _accessTokenExpiresUtc.AddSeconds(-60))
            return _accessToken;

        await _gate.WaitAsync(ct);
        try
        {
            if (_accessToken is not null && DateTime.UtcNow < _accessTokenExpiresUtc.AddSeconds(-60))
                return _accessToken;

            // Prefer the rotated token we got back from the last refresh; fall back to the store.
            var refreshToken = _currentRefreshToken
                ?? store.GetBetterwayRefreshToken()
                ?? throw new InvalidOperationException(
                    "No betterway refresh token available. Open the betterway app in Chrome and log in.");

            var form = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("refresh_token", refreshToken)
            });

            using var request = new HttpRequestMessage(HttpMethod.Post, "Token") { Content = form };
            request.Headers.Add("Origin", "https://app.betterway.co.il");
            request.Headers.Add("Referer", "https://app.betterway.co.il/");

            using var response = await http.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                throw new InvalidOperationException(
                    $"Betterway token refresh failed: {(int)response.StatusCode} {response.ReasonPhrase}. Body: {body}");
            }

            var payload = await response.Content.ReadFromJsonAsync<BetterwayTokenResponse>(cancellationToken: ct)
                ?? throw new InvalidOperationException("Empty token response");

            _accessToken = payload.AccessToken;
            _accessTokenExpiresUtc = DateTime.UtcNow.AddSeconds(payload.ExpiresIn);
            _lastRefreshTokenUsed = refreshToken;

            if (!string.IsNullOrEmpty(payload.RefreshToken))
                _currentRefreshToken = payload.RefreshToken;

            return _accessToken;
        }
        finally
        {
            _gate.Release();
        }
    }
}