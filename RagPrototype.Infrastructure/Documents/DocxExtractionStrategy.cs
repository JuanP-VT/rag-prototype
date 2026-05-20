using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace RagPrototype.Infrastructure.Documents;

public class DocxExtractionStrategy : IDocumentExtractionStrategy
{
    public bool Supports(string extension) =>
        extension.Equals(".docx", StringComparison.OrdinalIgnoreCase);

    public Task<string> ExtractAsync(string filePath, CancellationToken ct = default)
    {
        using var doc = WordprocessingDocument.Open(filePath, isEditable: false);
        var body = doc.MainDocumentPart?.Document?.Body;

        if (body is null)
            return Task.FromResult(string.Empty);

        var text = string.Join("\n", body
            .Descendants<Paragraph>()
            .Select(p => p.InnerText)
            .Where(t => !string.IsNullOrWhiteSpace(t)));

        return Task.FromResult(text);
    }
}
