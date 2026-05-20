using RagPrototype.Application.Features.Chat;
using RagPrototype.Domain.Entities;
using FluentAssertions;

namespace RagPrototype.Tests.Unit.Features.Chat;

public class PromptBuilderTests
{
    private static SearchResult MakeResult(string source, string content) =>
        new() { Chunk = new KnowledgeChunk { SourceDocument = source, TextContent = content, EmbeddingVector = [] }, SimilarityScore = 0.9f };

    [Fact]
    public void Build_AlwaysIncludesTheQuestionVerbatim()
    {
        const string question = "¿Cómo puedo construir un fondo de emergencia?";

        var prompt = PromptBuilder.Build(question, [MakeResult("doc.txt", "contenido")]);

        prompt.Should().Contain(question);
    }

    [Fact]
    public void Build_IncludesSourceDocumentNameInContextBlock()
    {
        var prompt = PromptBuilder.Build("pregunta", [MakeResult("doc_finanzas.txt", "contenido")]);

        prompt.Should().Contain("doc_finanzas.txt");
    }

    [Fact]
    public void Build_IncludesChunkTextInContextBlock()
    {
        const string chunkText = "Ahorra el 10% de tu ingreso mensual para emergencias.";

        var prompt = PromptBuilder.Build("pregunta", [MakeResult("doc.txt", chunkText)]);

        prompt.Should().Contain(chunkText);
    }

    [Fact]
    public void Build_WithMultipleChunks_IncludesAllSourcesAndContent()
    {
        IReadOnlyList<SearchResult> results =
        [
            MakeResult("doc_finanzas.txt", "Contenido financiero"),
            MakeResult("doc_bienestar.txt", "Contenido de bienestar"),
        ];

        var prompt = PromptBuilder.Build("pregunta", results);

        prompt.Should().Contain("doc_finanzas.txt")
              .And.Contain("doc_bienestar.txt")
              .And.Contain("Contenido financiero")
              .And.Contain("Contenido de bienestar");
    }
}
