using RagPrototype.Application.Common;
using RagPrototype.Domain.Entities;
using RagPrototype.Infrastructure.VectorMath;
namespace RagPrototype.Infrastructure.VectorStore;

public class InMemoryVectorStore : IVectorStore
{
    private readonly List<KnowledgeChunk> _chunks = [];
    private readonly object _lock = new();

    public void AddChunks(IEnumerable<KnowledgeChunk> chunks)
    {
        lock (_lock)
        {
            _chunks.AddRange(chunks);
        }
    }

    public IReadOnlyList<KnowledgeChunk> GetAll()
    {
        lock (_lock)
        {
            // Retornamos una copia (ToList) para prevenir que la colección sea 
            // modificada por otro hilo mientras la similitud coseno la está iterando.
            return _chunks.ToList();
        }
    }

    public IReadOnlyList<SearchResult> Search(float[] queryEmbedding, int topK, float threshold)
    {
        // GetAll() libera el lock antes de entrar al loop coseno: evitamos bloquear
        // otros hilos durante el cálculo más costoso de la clase.
        var all = GetAll();

        return all
            .Select(chunk => new SearchResult
            {
                Chunk = chunk,
                SimilarityScore = CosineSimilarity.Calculate(chunk.EmbeddingVector, queryEmbedding)
            })
            .Where(r => r.SimilarityScore >= threshold)
            .OrderByDescending(r => r.SimilarityScore)
            .Take(topK)
            .ToList();
    }
}