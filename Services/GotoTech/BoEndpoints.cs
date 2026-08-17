namespace Reports.Services.GotoTech;

public enum BoRegion
{
    Car2Go,
    Autotel
}

public static class BoEndpoints
{
    public const string CredentialsSessionKey = "ngStorage-credentials";

    // The tab the extension reads session storage FROM
    private static readonly IReadOnlyDictionary<BoRegion, string> Origins =
        new Dictionary<BoRegion, string>
        {
            [BoRegion.Car2Go]  = "https://car2gobo.gototech.co",
            [BoRegion.Autotel] = "https://prodautotelbo.gototech.co"
        };

    // The actual API endpoint requests are sent TO
    private static readonly IReadOnlyDictionary<BoRegion, string> ApiUrls =
        new Dictionary<BoRegion, string>
        {
            [BoRegion.Car2Go]  = "https://car2gopublicapi.gototech.co/API/SEND",
            [BoRegion.Autotel] = "https://autotelpublicapiprod.gototech.co/API/SEND"
        };

    public static string GetOrigin(BoRegion region) =>
        Origins.TryGetValue(region, out var o) ? o
            : throw new ArgumentOutOfRangeException(nameof(region), region, null);

    public static string GetApiUrl(BoRegion region) =>
        ApiUrls.TryGetValue(region, out var u) ? u
            : throw new ArgumentOutOfRangeException(nameof(region), region, null);
}