using RagPrototype.Infrastructure.Documents;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using FluentAssertions;

namespace RagPrototype.Tests.Unit.Infrastructure.Documents;

public class DocxExtractionStrategyTests : IDisposable
{
    private readonly List<string> _tempFiles = [];
    private readonly DocxExtractionStrategy _sut = new();

    public void Dispose()
    {
        foreach (var file in _tempFiles.Where(File.Exists))
            File.Delete(file);
    }

    [Theory]
    [InlineData(".docx")]
    [InlineData(".DOCX")]
    [InlineData(".Docx")]
    public void Supports_DocxExtensionVariants_ReturnsTrue(string extension)
    {
        _sut.Supports(extension).Should().BeTrue();
    }

    [Theory]
    [InlineData(".txt")]
    [InlineData(".pdf")]
    [InlineData("")]
    public void Supports_NonDocxExtensions_ReturnsFalse(string extension)
    {
        _sut.Supports(extension).Should().BeFalse();
    }

    [Fact]
    public async Task ExtractAsync_WithSingleParagraph_ReturnsText()
    {
        var path = CreateTempDocx("Hola desde docx");

        var result = await _sut.ExtractAsync(path, TestContext.Current.CancellationToken);

        result.Should().Be("Hola desde docx");
    }

    [Fact]
    public async Task ExtractAsync_WithMultipleParagraphs_ReturnsNewlineJoined()
    {
        var path = CreateTempDocx("Primer párrafo", "Segundo párrafo", "Tercer párrafo");

        var result = await _sut.ExtractAsync(path, TestContext.Current.CancellationToken);

        result.Should().Be("Primer párrafo\nSegundo párrafo\nTercer párrafo");
    }

    [Fact]
    public async Task ExtractAsync_WithBlankParagraphs_SkipsBlanks()
    {
        var path = CreateTempDocx("Primero", "   ", "Tercero");

        var result = await _sut.ExtractAsync(path, TestContext.Current.CancellationToken);

        result.Should().Be("Primero\nTercero");
    }

    [Fact]
    public async Task ExtractAsync_WithEmptyDocument_ReturnsEmptyString()
    {
        var path = CreateTempDocx();

        var result = await _sut.ExtractAsync(path, TestContext.Current.CancellationToken);

        result.Should().BeEmpty();
    }

    private string CreateTempDocx(params string[] paragraphs)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.docx");
        _tempFiles.Add(path);

        using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
        var mainPart = doc.AddMainDocumentPart();
        var body = new Body();

        foreach (var text in paragraphs)
            body.AppendChild(new Paragraph(new Run(new Text(text))));

        mainPart.Document = new Document(body);
        mainPart.Document.Save();

        return path;
    }
}
