namespace RagPrototype.Domain.Entities;

public record SearchResult
{
    public KnowledgeChunk Chunk { get; init; } = default!;
    public float SimilarityScore { get; init; }
}
