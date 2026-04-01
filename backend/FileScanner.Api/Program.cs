using FileScanner.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

builder.Services.AddSingleton<PdfService>();
builder.Services.AddSingleton<OcrService>();
builder.Services.AddSingleton<ExtractionOrchestrator>();

// ─── LLM provider selection ────────────────────────────────────────────────────────
var llmOptions = builder.Configuration.GetSection("Llm").Get<LlmOptions>()
    ?? new LlmOptions();

switch (llmOptions.Provider.ToLowerInvariant())
{
    case "openai":
    case "openrouter":
    case "gemini":
        builder.Services.AddHttpClient<OpenAiCompatibleLlmService>();
        builder.Services.AddSingleton<ILlmService>(sp =>
            new OpenAiCompatibleLlmService(
                llmOptions.Provider == "openai" ? llmOptions.OpenAi
                : llmOptions.Provider == "openrouter" ? llmOptions.OpenRouter
                : llmOptions.Gemini,
                sp.GetRequiredService<IHttpClientFactory>()
                  .CreateClient(nameof(OpenAiCompatibleLlmService))));
        break;

    case "ollama":
    default:
        builder.Services.AddSingleton<ILlmService>(_ => new OllamaLlmService(llmOptions));
        break;
}
// ─────────────────────────────────────────────────────────────────────────────────

var app = builder.Build();
app.UseCors();
app.MapControllers();
app.Run();
