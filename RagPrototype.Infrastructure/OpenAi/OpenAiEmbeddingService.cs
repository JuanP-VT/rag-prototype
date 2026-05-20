using RagPrototype.Application.Common;
using RagPrototype.Domain.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Embeddings;

namespace RagPrototype.Infrastructure.OpenAi;

public class OpenAiEmbeddingService(
    EmbeddingClient embeddingClient,
    ILogger<OpenAiEmbeddingService> logger) : IEmbeddingService
{
    public async Task<Result<float[]>> GetEmbeddingAsync(string text, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return Result.Failure<float[]>(Error.Validation("text", "El texto no puede estar vacío."));

        try
        {
            var response = await embeddingClient.GenerateEmbeddingAsync(text, cancellationToken: ct);
            var vector = response.Value.ToFloats().ToArray();

            logger.LogDebug("Embedding generado. Dimensiones: {Dimensions}", vector.Length);

            return Result.Success(vector);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error al generar embedding con OpenAI.");
            return Result.Failure<float[]>(Error.LlmUnavailable(ex.Message));
        }
    }
}