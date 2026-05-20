using RagPrototype.Domain.Common;

namespace RagPrototype.Application.Common;

public interface IEmbeddingService
{
    Task<Result<float[]>> GetEmbeddingAsync(string text, CancellationToken ct = default);
}