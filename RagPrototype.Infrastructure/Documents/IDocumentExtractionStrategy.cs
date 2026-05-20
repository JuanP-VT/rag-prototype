namespace RagPrototype.Infrastructure.Documents;

public interface IDocumentExtractionStrategy
{
    bool Supports(string extension);
    Task<string> ExtractAsync(string filePath, CancellationToken ct = default);
}
