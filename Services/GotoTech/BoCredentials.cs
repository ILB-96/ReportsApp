using System.Text.Json.Serialization;

namespace Reports.Services.GotoTech;

public sealed class BoCredentials
{
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; init; }

    [JsonPropertyName("token")]
    public string? Token { get; init; }

    [JsonPropertyName("remember")]
    public bool Remember { get; init; }
}