namespace RagPrototype.Application.Common;

public sealed class KnowledgeBaseOptions
{
    public const string SectionName = "KnowledgeBase";

    public string DocumentsPath { get; init; } = "Documents";
    public int ChunkSize { get; init; } = 500;
    public int ChunkOverlap { get; init; } = 50;
    public int TopK { get; init; } = 3;
    public float SimilarityThreshold { get; init; } = 0.35f;
}