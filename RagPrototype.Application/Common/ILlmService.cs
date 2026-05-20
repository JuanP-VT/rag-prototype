using RagPrototype.Domain.Common;

namespace RagPrototype.Application.Common;

public interface ILlmService
{
    Task<Result<string>> GenerateAnswerAsync(string prompt, CancellationToken ct = default);
}