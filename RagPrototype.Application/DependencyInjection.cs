using RagPrototype.Application.Common;
using RagPrototype.Application.Features.Ingestion;
using Microsoft.Extensions.DependencyInjection;
using RagPrototype.Application.Features.Chat;

namespace RagPrototype.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddOptions<KnowledgeBaseOptions>()
            .BindConfiguration(KnowledgeBaseOptions.SectionName)
            .ValidateOnStart();

        services.AddScoped<IIngestionService, IngestionService>();
        services.AddScoped<ChunkingService>();
        services.AddScoped<IChatService, ChatService>();

        return services;
    }
}