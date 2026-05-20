using RagPrototype.API.Exceptions;
using RagPrototype.Application;
using RagPrototype.Application.Features.Chat;
using RagPrototype.Application.Features.Ingestion;
using RagPrototype.Infrastructure;
using Scalar.AspNetCore;
using Serilog;
using RagPrototype.API.Extensions;
using Microsoft.AspNetCore.Mvc;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, lc) => lc
        .ReadFrom.Configuration(ctx.Configuration)
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
        .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7));

    builder.Services
        .AddApplication()
        .AddInfrastructure();

    builder.Services.AddOpenApi();
    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();
    builder.Services.AddCors(opt =>
        opt.AddPolicy("Default", p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

    var app = builder.Build();

    using (var scope = app.Services.CreateScope())
    {
        var ingestion = scope.ServiceProvider.GetRequiredService<IIngestionService>();
        var ingestionResult = await ingestion.IngestAllAsync(CancellationToken.None);
        if (ingestionResult.IsFailure)
            Log.Warning("La ingesta de documentos falló al iniciar: {Error}", ingestionResult.Error.Description);
    }

    app.UseExceptionHandler();
    app.UseSerilogRequestLogging();
    app.UseHttpsRedirection();
    app.UseCors("Default");

    app.MapOpenApi();
    app.MapScalarApiReference();
    var api = app.MapGroup("/api");

    api.MapPost("/chat", async ([FromBody] AskQuestionRequest request, [FromServices]IChatService chatService, CancellationToken ct) =>
    {
        var result = await chatService.AskAsync(request, ct);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.ToProblemDetails();
    })
    .WithName("AskQuestion")
    .WithDescription("Realiza una pregunta al asistente de bienestar financiero basándose en la base de conocimiento.");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application failed to start.");
}
finally
{
    Log.CloseAndFlush();
}

// Requerido para WebApplicationFactory<Program> en los tests de integración.
public partial class Program { }