using RagPrototype.Application.Common;
using RagPrototype.Application.Features.Ingestion;
using RagPrototype.Domain.Common;
using RagPrototype.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace RagPrototype.Tests.Unit.Features.Ingestion;

public class IngestionServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly IDocumentReader _documentReader;
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStore _vectorStore;
    private readonly IngestionService _sut;
    private readonly IOptions<KnowledgeBaseOptions> _options;

    public IngestionServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);

        _documentReader = Substitute.For<IDocumentReader>();
        _embeddingService = Substitute.For<IEmbeddingService>();
        _vectorStore = Substitute.For<IVectorStore>();

        _options = Options.Create(new KnowledgeBaseOptions
        {
            DocumentsPath = _tempDir,
            ChunkSize = 500,
            ChunkOverlap = 50,
        });

        _sut = BuildSut(_options);
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    [Fact]
    public async Task IngestAllAsync_WhenDirectoryDoesNotExist_ReturnsFailure()
    {
        var missingPath = Path.Combine(Path.GetTempPath(), "nonexistent_" + Guid.NewGuid());
        var sut = BuildSut(Options.Create(new KnowledgeBaseOptions { DocumentsPath = missingPath }));

        var result = await sut.IngestAllAsync(TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().EndWith(".NotFound");
    }

    [Fact]
    public async Task IngestAllAsync_WithEmptyDirectory_ReturnsSuccessWithoutStoringAnything()
    {
        var result = await _sut.IngestAllAsync(TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        _vectorStore.DidNotReceive().AddChunks(Arg.Any<IEnumerable<KnowledgeChunk>>());
    }

    [Fact]
    public async Task IngestAllAsync_WithDocument_EmbeddingPerChunkAndStoresAll()
    {
        var ct = TestContext.Current.CancellationToken;
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "doc1.txt"), "Short content", ct);

        _documentReader.ReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("Short content");
        _embeddingService.GetEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<float[]>([1f, 0f]));

        var result = await _sut.IngestAllAsync(ct);

        result.IsSuccess.Should().BeTrue();
        _vectorStore.Received(1).AddChunks(Arg.Is<IEnumerable<KnowledgeChunk>>(
            chunks => chunks.Any(c => c.SourceDocument == "doc1.txt")));
    }

    [Fact]
    public async Task IngestAllAsync_WhenEmbeddingFails_ReturnsFailureAndSkipsStore()
    {
        var ct = TestContext.Current.CancellationToken;
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "bad.txt"), "content", ct);

        _documentReader.ReadAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("content");
        _embeddingService.GetEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<float[]>(Error.LlmUnavailable("API timeout")));

        var result = await _sut.IngestAllAsync(ct);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Llm.Unavailable");
        _vectorStore.DidNotReceive().AddChunks(Arg.Any<IEnumerable<KnowledgeChunk>>());
    }

    private IngestionService BuildSut(IOptions<KnowledgeBaseOptions> options) =>
        new(new ChunkingService(options), _embeddingService, _vectorStore,
            _documentReader, options, NullLogger<IngestionService>.Instance);
}
