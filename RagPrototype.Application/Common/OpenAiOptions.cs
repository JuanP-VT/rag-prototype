namespace RagPrototype.Application.Common;
public sealed class OpenAiOptions
{
    public const string SectionName = "OpenAI";

    public string ApiKey { get; init; } = string.Empty;
    public string EmbeddingModel { get; init; } = "text-embedding-3-small";
    public string ChatModel { get; init; } = "gpt-4o-mini";
}