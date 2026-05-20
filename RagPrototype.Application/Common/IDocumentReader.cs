namespace RagPrototype.Application.Common;

public interface IDocumentReader
{
    Task<string> ReadAsync(string filePath, CancellationToken ct = default);
}