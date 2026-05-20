namespace RagPrototype.Domain.Entities;

public record RagResponse
{
    public string Answer { get; init; } = string.Empty;
    public bool IsFromKnowledgeBase { get; init; }
    public IReadOnlyList<string> SourceDocuments { get; init; } = [];
    public string ConstructedPrompt { get; init; } = string.Empty;
}
