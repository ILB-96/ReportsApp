namespace Reports.Utilities;

public interface IDocxTemplateGenerator
{
    Task<string> GenerateFromEmbeddedAsync(
        string embeddedResourceName,
        string outputPath,
        IDictionary<string, string> tokens,
        CancellationToken ct = default);
}