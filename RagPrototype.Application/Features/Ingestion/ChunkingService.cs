using RagPrototype.Application.Common;
using Microsoft.Extensions.Options;

namespace RagPrototype.Application.Features.Ingestion;

public class ChunkingService(IOptions<KnowledgeBaseOptions> options)
{
    private readonly KnowledgeBaseOptions _config = options.Value;

    /// <summary>
    /// Divide un texto largo en fragmentos más pequeños usando una ventana deslizante.
    /// Utiliza la configuración inyectada desde KnowledgeBaseOptions.
    /// </summary>
    public IReadOnlyList<string> ChunkText(string text)
    {
        int chunkSize = _config.ChunkSize;
        int overlap = _config.ChunkOverlap;

        if (string.IsNullOrWhiteSpace(text))
            return Array.Empty<string>();

        // [CRITERIO DE INGENIERÍA - FAIL FAST]
        // Se lanza una excepción porque un overlap >= chunkSize es un error de configuración 
        // estructural que causaría un bucle infinito y agotamiento de recursos.
        if (overlap >= chunkSize)
            throw new ArgumentException(
                $"Configuración inválida: Overlap ({overlap}) debe ser menor a ChunkSize ({chunkSize}).");

        if (text.Length <= chunkSize)
            return [text];

        var chunks = new List<string>();
        int position = 0;

        while (position < text.Length)
        {
            int length = Math.Min(chunkSize, text.Length - position);
            chunks.Add(text.Substring(position, length));

            if (position + length >= text.Length)
                break;

            position += (chunkSize - overlap);
        }

        return chunks;
    }
}