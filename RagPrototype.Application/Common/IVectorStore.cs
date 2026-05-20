namespace RagPrototype.Application.Common;

public interface IVectorStore
{
    void AddChunks(IEnumerable<KnowledgeChunk> chunks);
    IReadOnlyList<KnowledgeChunk> GetAll();
    IReadOnlyList<SearchResult> Search(float[] queryEmbedding, int topK, float threshold);
}