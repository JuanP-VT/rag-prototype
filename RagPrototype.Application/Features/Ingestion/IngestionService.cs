using RagPrototype.Application.Common;
using RagPrototype.Domain.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace RagPrototype.Application.Features.Ingestion;

public class IngestionService(
    ChunkingService chunkingService,
    IEmbeddingService embeddingService,
    IVectorStore vectorStore,
    IDocumentReader documentReader,
    IOptions<KnowledgeBaseOptions> options,
    ILogger<IngestionService> logger) : IIngestionService
{
    public async Task<Result> IngestAllAsync(CancellationToken ct = default)
    {
        var config = options.Value;
        var fullPath = Path.Combine(AppContext.BaseDirectory, config.DocumentsPath);
        if (!Directory.Exists(fullPath))
        {
            logger.LogWarning("El directorio de documentos no existe: {Path}", fullPath);
            return Result.Failure(Error.NotFound("KnowledgeBase.DocumentsPath", fullPath));
        }

        var files = Directory.GetFiles(fullPath, "*.*")
                 .Where(f => f.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ||
                 f.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                 .ToArray();

        if (files.Length == 0)
        {
            logger.LogInformation("No se encontraron archivos .txt en {Path}.", fullPath);
            return Result.Success();
        }

        var allChunks = new List<KnowledgeChunk>();

        foreach (var file in files)
        {
            var docName = Path.GetFileName(file);
            var text = await documentReader.ReadAsync(file, ct);
            logger.LogInformation("Texto extraído de {DocName}: {Length} caracteres.", docName, text.Length);
            logger.LogDebug("Preview de {DocName}: {Preview}", docName, text[..Math.Min(100, text.Length)]);

            var textChunks = chunkingService.ChunkText(text);

            foreach (var textChunk in textChunks)
            {
                var embeddingResult = await embeddingService.GetEmbeddingAsync(textChunk, ct);

                if (embeddingResult.IsFailure)
                {
                    logger.LogError("Fallo al obtener embedding para {DocName}: {Error}", docName, embeddingResult.Error.Description);
                    return Result.Failure(embeddingResult.Error);
                }

                allChunks.Add(new KnowledgeChunk
                {
                    SourceDocument = docName,
                    TextContent = textChunk,
                    EmbeddingVector = embeddingResult.Value
                });
            }

            logger.LogInformation("Ingested {ChunkCount} chunks from {Document}", textChunks.Count, docName);
        }

        vectorStore.AddChunks(allChunks);
        logger.LogInformation("Ingesta completada. {Total} chunks guardados en memoria.", allChunks.Count);

        return Result.Success();
    }
}