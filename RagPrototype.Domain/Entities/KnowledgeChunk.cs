namespace RagPrototype.Domain.Entities;

public record KnowledgeChunk
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string SourceDocument { get; init; } = string.Empty;
    public string TextContent { get; init; } = string.Empty;
    public float[] EmbeddingVector { get; init; } = Array.Empty<float>();
}
