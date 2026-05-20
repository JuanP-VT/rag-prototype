using RagPrototype.Application.Common;
using RagPrototype.Domain.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace RagPrototype.Infrastructure.OpenAi;

public class OpenAiLlmService(
    ChatClient chatClient,
    IOptions<OpenAiOptions> options,
    ILogger<OpenAiLlmService> logger) : ILlmService
{
    private readonly string _model = options.Value.ChatModel;

    public async Task<Result<string>> GenerateAnswerAsync(string prompt, CancellationToken ct = default)
    {
        try
        {
            logger.LogDebug("Enviando petición a OpenAI Chat (Modelo: {Model})", _model);

            var completionOptions = new ChatCompletionOptions
            {
                // Temperatura baja: el modelo debe ceñirse al contexto RAG provisto,
                // no "ser creativo". Valores altos causan alucinaciones fuera del documento.
                Temperature = 0.1f,
                MaxOutputTokenCount = 500
            };

            var completion = await chatClient.CompleteChatAsync([new UserChatMessage(prompt)], completionOptions, ct);

            if (completion is null || completion.Value.Content.Count == 0)
            {
                return Result.Failure<string>(Error.LlmUnavailable("El LLM no devolvió ninguna respuesta."));
            }

            var answer = completion.Value.Content[0].Text;

            return Result.Success(answer);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error crítico durante la generación con OpenAI.");
            return Result.Failure<string>(Error.LlmUnavailable($"Fallo en la API de OpenAI: {ex.Message}"));
        }
    }
}