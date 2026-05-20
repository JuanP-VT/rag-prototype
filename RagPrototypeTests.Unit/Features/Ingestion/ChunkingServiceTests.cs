using RagPrototype.Application.Common;
using RagPrototype.Application.Features.Ingestion;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace RagPrototype.Tests.Unit.Features.Ingestion;

public class ChunkingServiceTests
{
    private static ChunkingService Create(int chunkSize = 100, int overlap = 10) =>
        new(Options.Create(new KnowledgeBaseOptions { ChunkSize = chunkSize, ChunkOverlap = overlap }));

    [Fact]
    public void ChunkText_WithEmptyOrWhitespace_ReturnsEmpty()
    {
        var sut = Create();

        sut.ChunkText(string.Empty).Should().BeEmpty();
        sut.ChunkText("   ").Should().BeEmpty();
    }

    [Fact]
    public void ChunkText_WithTextShorterThanChunkSize_ReturnsSingleChunkWithFullText()
    {
        var sut = Create(chunkSize: 100);
        const string text = "Short document.";

        var result = sut.ChunkText(text);

        result.Should().ContainSingle().Which.Should().Be(text);
    }

    [Fact]
    public void ChunkText_WithLongText_ProducesCorrectChunkCount()
    {
        // chunkSize=10, overlap=2, step=8
        // positions: 0, 8, 16, 24 → 4 chunks for a 30-char text
        var sut = Create(chunkSize: 10, overlap: 2);
        var text = new string('a', 30);

        var result = sut.ChunkText(text);

        result.Should().HaveCount(4);
        result.Should().AllSatisfy(c => c.Length.Should().BeLessThanOrEqualTo(10));
    }

    [Fact]
    public void ChunkText_WithOverlap_SecondChunkStartsAtCorrectOffset()
    {
        // chunkSize=10, overlap=3, step=7
        // "ABCDEFGHIJKLMNOPQRST" (20 chars)
        //  chunk[0]: [0..9]  → "ABCDEFGHIJ"
        //  chunk[1]: [7..16] → starts with 'H' (the overlapping region)
        var sut = Create(chunkSize: 10, overlap: 3);
        const string text = "ABCDEFGHIJKLMNOPQRST";

        var result = sut.ChunkText(text);

        result[0].Should().Be("ABCDEFGHIJ");
        result[1].Should().StartWith("H");
    }

    [Fact]
    public void ChunkText_WhenOverlapEqualsChunkSize_ThrowsArgumentException()
    {
        var sut = Create(chunkSize: 50, overlap: 50);

        var act = () => sut.ChunkText("some text");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Overlap*");
    }
}
