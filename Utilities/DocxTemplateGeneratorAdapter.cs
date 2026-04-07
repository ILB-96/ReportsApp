namespace Reports.Utilities;
public interface IDocxTemplateGenerator
{
    Task<string> GenerateFromEmbeddedAsync(
        string embeddedResourceName,
        string outputPath,
        IDictionary<string, string> tokens,
        CancellationToken ct = default);
}
public sealed class DocxTemplateGeneratorAdapter : IDocxTemplateGenerator
{
    public Task<string> GenerateFromEmbeddedAsync(
        string embeddedResourceName,
        string outputPath,
        IDictionary<string, string> tokens,
        CancellationToken ct = default)
    {
        return DocxTemplateGenerator.GenerateFromEmbeddedAsync(embeddedResourceName, outputPath, tokens, ct);
    }
}