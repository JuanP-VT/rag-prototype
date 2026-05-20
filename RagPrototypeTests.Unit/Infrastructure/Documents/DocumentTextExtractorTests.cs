using RagPrototype.Infrastructure.Documents;
using FluentAssertions;
using NSubstitute;

namespace RagPrototype.Tests.Unit.Infrastructure.Documents;

public class DocumentTextExtractorTests
{
    [Fact]
    public async Task ReadAsync_WithMatchingStrategy_DelegatesToStrategy()
    {
        var strategy = Substitute.For<IDocumentExtractionStrategy>();
        strategy.Supports(".txt").Returns(true);
        strategy.ExtractAsync("file.txt", Arg.Any<CancellationToken>()).Returns("contenido extraído");
        var sut = new DocumentTextExtractor([strategy]);

        var result = await sut.ReadAsync("file.txt", TestContext.Current.CancellationToken);

        result.Should().Be("contenido extraído");
    }

    [Fact]
    public async Task ReadAsync_WithMultipleStrategies_UsesOnlyMatchingOne()
    {
        var txtStrategy = Substitute.For<IDocumentExtractionStrategy>();
        txtStrategy.Supports(".txt").Returns(true);
        txtStrategy.Supports(".docx").Returns(false);

        var docxStrategy = Substitute.For<IDocumentExtractionStrategy>();
        docxStrategy.Supports(".txt").Returns(false);
        docxStrategy.Supports(".docx").Returns(true);
        docxStrategy.ExtractAsync("file.docx", Arg.Any<CancellationToken>()).Returns("contenido docx");

        var sut = new DocumentTextExtractor([txtStrategy, docxStrategy]);

        var result = await sut.ReadAsync("file.docx", TestContext.Current.CancellationToken);

        result.Should().Be("contenido docx");
        await txtStrategy.DidNotReceive().ExtractAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ReadAsync_WithUnsupportedExtension_ThrowsNotSupportedException()
    {
        var strategy = Substitute.For<IDocumentExtractionStrategy>();
        strategy.Supports(Arg.Any<string>()).Returns(false);
        var sut = new DocumentTextExtractor([strategy]);

        var act = async () => await sut.ReadAsync("file.pdf", TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<NotSupportedException>()
            .WithMessage("*pdf*");
    }

    [Fact]
    public async Task ReadAsync_WithNoStrategies_ThrowsNotSupportedException()
    {
        var sut = new DocumentTextExtractor([]);

        var act = async () => await sut.ReadAsync("file.txt", TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<NotSupportedException>();
    }

    [Fact]
    public async Task ReadAsync_ExtensionMatchingIsCaseInsensitive()
    {
        var strategy = Substitute.For<IDocumentExtractionStrategy>();
        strategy.Supports(".txt").Returns(true);
        strategy.ExtractAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("ok");
        var sut = new DocumentTextExtractor([strategy]);

        var result = await sut.ReadAsync("FILE.TXT", TestContext.Current.CancellationToken);

        result.Should().Be("ok");
    }
}
