using RagPrototype.Domain.Common;

namespace RagPrototype.Application.Features.Ingestion;

public interface IIngestionService
{
    Task<Result> IngestAllAsync(CancellationToken ct = default);
}