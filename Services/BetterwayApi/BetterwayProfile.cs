namespace Reports.Services.BetterwayApi;

public enum BetterwayProfile
{
    Colmobil = 28121,
    Lease = 36401,
    Goto = 14427,
    Autotel = 14426
}

public static class BetterwayProfileResolver
{
    public static BetterwayProfile Resolve(string profileName)
    {
        if (string.IsNullOrWhiteSpace(profileName))
            throw new ArgumentException("Profile name must not be empty.", nameof(profileName));

        return profileName.Trim().ToLowerInvariant() switch
        {
            "colmobil" => BetterwayProfile.Colmobil,
            "lease"    => BetterwayProfile.Lease,
            "goto"     => BetterwayProfile.Goto,
            "autotel"  => BetterwayProfile.Autotel,
            _ => throw new InvalidOperationException($"Unknown profile name: '{profileName}'.")
        };
    }
}