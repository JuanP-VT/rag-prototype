using RagPrototype.Application.Common;
using RagPrototype.Application.Features.Chat;
using RagPrototype.Domain.Common;
using RagPrototype.Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace RagPrototype.Tests.Unit.Features.Chat;

public class ChatServiceTests
{
    private readonly IEmbeddingService _embeddingService = Substitute.For<IEmbeddingService>();
    private readonly IVectorStore _vectorStore = Substitute.For<IVectorStore>();
    private readonly ILlmService _llmService = Substitute.For<ILlmService>();
    private readonly ChatService _sut;

    public ChatServiceTests()
    {
        var options = Options.Create(new KnowledgeBaseOptions { TopK = 3, SimilarityThreshold = 0.5f });
        _sut = new ChatService(_embeddingService, _vectorStore, _llmService, options, NullLogger<ChatService>.Instance);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task AskAsync_WithEmptyOrWhitespaceQuestion_ReturnsValidationFailure(string question)
    {
        var result = await _sut.AskAsync(new AskQuestionRequest(question), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().StartWith("Validation");
    }

    [Fact]
    public async Task AskAsync_WhenEmbeddingServiceFails_PropagatesFailureWithoutCallingSearch()
    {
        _embeddingService.GetEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<float[]>(Error.LlmUnavailable("API timeout")));

        var result = await _sut.AskAsync(new AskQuestionRequest("¿Cómo ahorro?"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Llm.Unavailable");
        _vectorStore.DidNotReceive().Search(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<float>());
    }

    [Fact]
    public async Task AskAsync_WhenNoChunksMeetThreshold_ReturnsFallbackWithoutCallingLlm()
    {
        _embeddingService.GetEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<float[]>([1f, 0f]));
        _vectorStore.Search(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<float>())
            .Returns(Array.Empty<SearchResult>());

        var result = await _sut.AskAsync(new AskQuestionRequest("¿Cómo ahorro?"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsFromKnowledgeBase.Should().BeFalse();
        _ = _llmService.DidNotReceive().GenerateAnswerAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AskAsync_WithRelevantChunks_ReturnsAnswerWithSourceDocumentsAndPopulatedPrompt()
    {
        var chunk = new KnowledgeChunk
        {
            SourceDocument = "doc_finanzas.txt",
            TextContent = "Ahorra el 10% de tu ingreso.",
            EmbeddingVector = [1f, 0f]
        };
        var searchResult = new SearchResult { Chunk = chunk, SimilarityScore = 0.9f };

        _embeddingService.GetEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<float[]>([1f, 0f]));
        _vectorStore.Search(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<float>())
            .Returns(new[] { searchResult });
        _llmService.GenerateAnswerAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success("Debes ahorrar el 10% mensual."));

        var result = await _sut.AskAsync(new AskQuestionRequest("¿Cómo ahorro?"), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Value.IsFromKnowledgeBase.Should().BeTrue();
        result.Value.Answer.Should().Be("Debes ahorrar el 10% mensual.");
        result.Value.SourceDocuments.Should().Contain("doc_finanzas.txt");
        result.Value.ConstructedPrompt.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task AskAsync_WhenLlmFails_PropagatesLlmFailure()
    {
        var chunk = new KnowledgeChunk { SourceDocument = "doc.txt", TextContent = "content", EmbeddingVector = [1f, 0f] };
        var searchResult = new SearchResult { Chunk = chunk, SimilarityScore = 0.9f };

        _embeddingService.GetEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success<float[]>([1f, 0f]));
        _vectorStore.Search(Arg.Any<float[]>(), Arg.Any<int>(), Arg.Any<float>())
            .Returns(new[] { searchResult });
        _llmService.GenerateAnswerAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<string>(Error.LlmUnavailable("rate limit exceeded")));

        var result = await _sut.AskAsync(new AskQuestionRequest("¿Cómo ahorro?"), TestContext.Current.CancellationToken);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Llm.Unavailable");
    }
}
