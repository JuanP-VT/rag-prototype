using RagPrototype.Application.Common;
using RagPrototype.Domain.Common;
using RagPrototype.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace RagPrototype.Application.Features.Chat;

public class ChatService(
    IEmbeddingService embeddingService,
    IVectorStore vectorStore,
    ILlmService llmService,
    IOptions<KnowledgeBaseOptions> options,
    ILogger<ChatService> logger) : IChatService
{
    public async Task<Result<RagResponse>> AskAsync(AskQuestionRequest request, CancellationToken ct = default)
    {
        var config = options.Value;

        if (string.IsNullOrWhiteSpace(request.Question))
        {
            logger.LogWarning("Se recibió una pregunta vacía o con solo espacios.");
            return Result.Failure<RagResponse>(Error.Validation("Question", "La pregunta no puede estar vacía."));
        }

        var embeddingResult = await embeddingService.GetEmbeddingAsync(request.Question, ct);

        if (embeddingResult.IsFailure)
        {
            logger.LogError("Fallo al generar el embedding para la pregunta: {Error}", embeddingResult.Error.Description);
            return Result.Failure<RagResponse>(embeddingResult.Error);
        }

        var queryVector = embeddingResult.Value;

        var searchResults = vectorStore.Search(queryVector, config.TopK, config.SimilarityThreshold);

        // Si no hay resultados, significa que la base está vacía o ningún chunk superó el umbral.
        // Cumplimos la regla de negocio: cortocircuito elegante sin fallar la petición HTTP.
        if (searchResults.Count == 0)
        {
            logger.LogWarning("Ningún resultado superó el umbral de {Threshold:F4} o la base vectorial está vacía.",
                config.SimilarityThreshold);
            return Result.Success(CreateFallbackResponse());
        }

        foreach (var result in searchResults)
        {
            logger.LogDebug("Chunk recuperado de {Document} con puntaje {Score:F4}",
                result.Chunk.SourceDocument, result.SimilarityScore);
        }

        var bestScore = searchResults[0].SimilarityScore;
        logger.LogInformation("Umbral alcanzado ({Score:F4}). Avanzando a la Fase 4 con los {Count} mejores chunks.",
            bestScore, searchResults.Count);

        var constructedPrompt = PromptBuilder.Build(request.Question, searchResults);

        logger.LogDebug("Prompt construido para el LLM:\n{Prompt}", constructedPrompt);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var llmResult = await llmService.GenerateAnswerAsync(constructedPrompt, ct);
        stopwatch.Stop();

        if (llmResult.IsFailure)
        {
            logger.LogError("Fallo en la llamada al LLM: {Error}", llmResult.Error.Description);
            return Result.Failure<RagResponse>(llmResult.Error);
        }

        logger.LogInformation("LLM responded in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
        var finalAnswer = llmResult.Value;

        return Result.Success(new RagResponse
        {
            Answer = finalAnswer,
            IsFromKnowledgeBase = true,
            SourceDocuments = searchResults.Select(r => r.Chunk.SourceDocument).Distinct().ToList(),
            ConstructedPrompt = constructedPrompt // Siempre poblado para el revisor
        });
    }
    

    /// <summary>
    /// Crea la respuesta de respaldo cuando la base de conocimiento no tiene contexto relevante.
    /// </summary>
    private static RagResponse CreateFallbackResponse() => new()
    {
        Answer = "No tengo información sobre eso en mi base de conocimiento.",
        IsFromKnowledgeBase = false,
        SourceDocuments = [],
        ConstructedPrompt = "[Umbral no alcanzado o base vectorial vacía. Llamada al LLM abortada.]"
    };
}