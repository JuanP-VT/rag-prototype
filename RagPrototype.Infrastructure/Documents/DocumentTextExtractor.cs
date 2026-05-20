using RagPrototype.Application.Common;

namespace RagPrototype.Infrastructure.Documents;

public class DocumentTextExtractor(IEnumerable<IDocumentExtractionStrategy> strategies) : IDocumentReader
{
    public async Task<string> ReadAsync(string filePath, CancellationToken ct = default)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        var strategy = strategies.FirstOrDefault(s => s.Supports(extension))
            ?? throw new NotSupportedException($"Formato no soportado: {extension}");

        return await strategy.ExtractAsync(filePath, ct);
    }
}
