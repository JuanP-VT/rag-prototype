using RagPrototype.Infrastructure.Documents;
using FluentAssertions;

namespace RagPrototype.Tests.Unit.Infrastructure.Documents;

public class TxtExtractionStrategyTests : IDisposable
{
    private readonly string _tempFile;
    private readonly TxtExtractionStrategy _sut = new();

    public TxtExtractionStrategyTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.txt");
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile))
            File.Delete(_tempFile);
    }

    [Theory]
    [InlineData(".txt")]
    [InlineData(".TXT")]
    [InlineData(".Txt")]
    public void Supports_TxtExtensionVariants_ReturnsTrue(string extension)
    {
        _sut.Supports(extension).Should().BeTrue();
    }

    [Theory]
    [InlineData(".docx")]
    [InlineData(".pdf")]
    [InlineData("")]
    public void Supports_NonTxtExtensions_ReturnsFalse(string extension)
    {
        _sut.Supports(extension).Should().BeFalse();
    }

    [Fact]
    public async Task ExtractAsync_WithContent_ReturnsFileContent()
    {
        const string expected = "Hola desde txt";
        await File.WriteAllTextAsync(_tempFile, expected, TestContext.Current.CancellationToken);

        var result = await _sut.ExtractAsync(_tempFile, TestContext.Current.CancellationToken);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task ExtractAsync_WithEmptyFile_ReturnsEmptyString()
    {
        await File.WriteAllTextAsync(_tempFile, string.Empty, TestContext.Current.CancellationToken);

        var result = await _sut.ExtractAsync(_tempFile, TestContext.Current.CancellationToken);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ExtractAsync_WithMultilineContent_PreservesLineBreaks()
    {
        const string expected = "Línea uno\nLínea dos\nLínea tres";
        await File.WriteAllTextAsync(_tempFile, expected, TestContext.Current.CancellationToken);

        var result = await _sut.ExtractAsync(_tempFile, TestContext.Current.CancellationToken);

        result.Should().Be(expected);
    }
}
