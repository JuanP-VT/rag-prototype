using RagPrototype.Application.Common;
using RagPrototype.Infrastructure.Documents;
using RagPrototype.Infrastructure.OpenAi;
using RagPrototype.Infrastructure.VectorStore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using OpenAI.Embeddings;
using OpenAI;
namespace RagPrototype.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {

        services.AddOptions<OpenAiOptions>()
            .BindConfiguration(OpenAiOptions.SectionName)
            .ValidateOnStart();

        services.AddScoped<IDocumentExtractionStrategy, TxtExtractionStrategy>();
        services.AddScoped<IDocumentExtractionStrategy, DocxExtractionStrategy>();
        services.AddScoped<IDocumentReader, DocumentTextExtractor>();
        // Singleton: el vector store ES la base de datos en RAM; un Scoped crearía una
        // instancia vacía por request y perdería todos los chunks ingestados al inicio.
        services.AddSingleton<IVectorStore, InMemoryVectorStore>();
        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<OpenAiOptions>>().Value;

            // [CRITERIO DE INGENIERÍA - FAIL FAST]
            if (string.IsNullOrWhiteSpace(options.ApiKey))
                throw new ArgumentException("La API Key de OpenAI está vacía en la configuración.");

            if (string.IsNullOrWhiteSpace(options.EmbeddingModel))
                throw new ArgumentException("El EmbeddingModel está vacío en la configuración. Revisa el appsettings.json.");

            var client = new OpenAIClient(options.ApiKey);
            return client.GetEmbeddingClient(options.EmbeddingModel);
        });

        services.AddSingleton(sp =>
        {
            var options = sp.GetRequiredService<IOptions<OpenAiOptions>>().Value;
            return new ChatClient(model: options.ChatModel, apiKey: options.ApiKey);
        });

        services.AddScoped<IEmbeddingService, OpenAiEmbeddingService>();
        services.AddScoped<ILlmService, OpenAiLlmService>();

        return services;
    }
}