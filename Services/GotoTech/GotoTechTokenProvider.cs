namespace Reports.Services.GotoTech;
using System.Text.Json;
using Reports.Services.ChromeSync;
public interface IGotoTechTokenProvider
{
    /// <summary>
    /// Returns the full parsed credentials object, or null if the tab is not
    /// open / not yet synced.
    /// </summary>
    BoCredentials? GetCredentials(BoRegion region);

    /// <summary>
    /// Returns just the token string.
    /// Throws <see cref="InvalidOperationException"/> if the token is missing.
    /// </summary>
    string GetToken(BoRegion region);

    bool HasToken(BoRegion region);
}

public sealed class GotoTechTokenProvider(ChromeSyncStore store) : IGotoTechTokenProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public BoCredentials? GetCredentials(BoRegion region)
    {
        var origin = BoEndpoints.GetOrigin(region);
        var raw = store.GetSessionStorageValue(origin, BoEndpoints.CredentialsSessionKey);
        Console.WriteLine($"Origin: {origin} Raw: {raw}");
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        try
        {
            return JsonSerializer.Deserialize<BoCredentials>(raw, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public string GetToken(BoRegion region)
    {
        var token = GetCredentials(region)?.Token;
        Console.WriteLine($"Token2: {token}");

        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException(
                $"No token found for {region} back-office. " +
                $"Make sure the {BoEndpoints.GetOrigin(region)} tab is open and signed in " +
                $"so the Chrome extension can sync '{BoEndpoints.CredentialsSessionKey}'.");

        return token;
    }

    public bool HasToken(BoRegion region) =>
        !string.IsNullOrWhiteSpace(GetCredentials(region)?.Token);
}