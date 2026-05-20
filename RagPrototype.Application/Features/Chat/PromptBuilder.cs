using System.Text;
using RagPrototype.Domain.Entities;

namespace RagPrototype.Application.Features.Chat;

public static class PromptBuilder
{
    private const string Template = """
        Eres un asistente de bienestar financiero. Responde ÚNICAMENTE basándote en el contexto proporcionado.
        Si el contexto no contiene suficiente información, indícalo claramente.

        === CONTEXTO ===
        {context_block}

        === PREGUNTA ===
        {question}

        === INSTRUCCIONES ===
        - Responde en español, de forma clara y concisa.
        - Cita el documento fuente cuando sea relevante.
        - Si no puedes responder con el contexto dado, di: "No tengo información sobre eso en mi base de conocimiento."
        """;

    public static string Build(string question, IReadOnlyList<SearchResult> searchResults)
    {
        var contextBuilder = new StringBuilder();

        foreach (var result in searchResults)
        {
            contextBuilder.AppendLine($"[Fuente: {result.Chunk.SourceDocument}]");
            contextBuilder.AppendLine(result.Chunk.TextContent);
            contextBuilder.AppendLine();
        }

        return Template
            .Replace("{context_block}", contextBuilder.ToString().TrimEnd())
            .Replace("{question}", question);
    }
}