namespace RagPrototype.Infrastructure.Documents;

public class TxtExtractionStrategy : IDocumentExtractionStrategy
{
    public bool Supports(string extension) =>
        extension.Equals(".txt", StringComparison.OrdinalIgnoreCase);

    public async Task<string> ExtractAsync(string filePath, CancellationToken ct = default) =>
        await File.ReadAllTextAsync(filePath, ct);
}
