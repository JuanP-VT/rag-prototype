using RagPrototype.Domain.Entities;
using RagPrototype.Infrastructure.VectorStore;
using FluentAssertions;

namespace RagPrototype.Tests.Unit.Infrastructure;

public class InMemoryVectorStoreTests
{
    private static KnowledgeChunk MakeChunk(float[] vector, string source = "doc.txt") =>
        new() { SourceDocument = source, TextContent = "content", EmbeddingVector = vector };

    [Fact]
    public void GetAll_WhenEmpty_ReturnsEmptyList()
    {
        var sut = new InMemoryVectorStore();

        sut.GetAll().Should().BeEmpty();
    }

    [Fact]
    public void AddChunks_ThenGetAll_ReturnsAllAddedChunks()
    {
        var sut = new InMemoryVectorStore();
        var chunks = new[] { MakeChunk([1f, 0f], "a.txt"), MakeChunk([0f, 1f], "b.txt") };

        sut.AddChunks(chunks);

        sut.GetAll().Should().HaveCount(2)
            .And.Contain(c => c.SourceDocument == "a.txt")
            .And.Contain(c => c.SourceDocument == "b.txt");
    }

    [Fact]
    public void Search_ExcludesChunksBelowThreshold()
    {
        var sut = new InMemoryVectorStore();
        // [1,0] vs query [1,0] → similarity 1.0 (included)
        // [0,1] vs query [1,0] → similarity 0.0 (excluded below 0.5)
        sut.AddChunks([MakeChunk([1f, 0f], "above.txt"), MakeChunk([0f, 1f], "below.txt")]);

        var results = sut.Search([1f, 0f], topK: 5, threshold: 0.5f);

        results.Should().ContainSingle()
            .Which.Chunk.SourceDocument.Should().Be("above.txt");
    }

    [Fact]
    public void Search_RespectsTopKLimit()
    {
        var sut = new InMemoryVectorStore();
        sut.AddChunks(Enumerable.Range(1, 5).Select(i => MakeChunk([1f, 0f], $"doc{i}.txt")));

        var results = sut.Search([1f, 0f], topK: 3, threshold: 0f);

        results.Should().HaveCount(3);
    }

    [Fact]
    public void Search_ResultsAreOrderedByDescendingScore()
    {
        var sut = new InMemoryVectorStore();
        // [0.707, 0.707] vs [1, 0] → cosine ≈ 0.707; [1, 0] vs [1, 0] → cosine = 1.0
        sut.AddChunks([MakeChunk([0.707f, 0.707f], "medium.txt"), MakeChunk([1f, 0f], "high.txt")]);

        var results = sut.Search([1f, 0f], topK: 5, threshold: 0f);

        results[0].Chunk.SourceDocument.Should().Be("high.txt");
        results[1].Chunk.SourceDocument.Should().Be("medium.txt");
        results[0].SimilarityScore.Should().BeGreaterThan(results[1].SimilarityScore);
    }
}
