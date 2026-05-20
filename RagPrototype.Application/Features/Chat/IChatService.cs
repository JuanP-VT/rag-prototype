using RagPrototype.Domain.Common;

namespace RagPrototype.Application.Features.Chat;

public record AskQuestionRequest(string Question);

public interface IChatService
{
    Task<Result<RagResponse>> AskAsync(AskQuestionRequest request, CancellationToken ct = default);
}