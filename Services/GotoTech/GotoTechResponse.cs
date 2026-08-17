using System.Text.Json;
using System.Text.Json.Serialization;

namespace Reports.Services.GotoTech;

public sealed class GotoTechResponse
{
    [JsonPropertyName("Status")]
    public int Status { get; init; }

    [JsonPropertyName("Data")]
    public string? Data { get; init; }

    [JsonPropertyName("HttpStatusCode")]
    public int HttpStatusCode { get; init; }

    [JsonPropertyName("ErrorMessage")]
    public string? ErrorMessage { get; init; }

    [JsonPropertyName("MessageId")]
    public int MessageId { get; init; }

    [JsonPropertyName("serverMessage")]
    public string? ServerMessage { get; init; }

    public bool IsSuccess => Status == 0 && string.IsNullOrWhiteSpace(ErrorMessage);

    // Data is double-serialized (JSON string containing JSON), same as the maintenance feed.
    public T? DeserializeData<T>(JsonSerializerOptions? options = null) =>
        string.IsNullOrWhiteSpace(Data)
            ? default
            : JsonSerializer.Deserialize<T>(Data,
                options ?? new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
}