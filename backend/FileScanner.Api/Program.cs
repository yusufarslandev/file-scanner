using System.Text;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.EntityFrameworkCore;
using FileScanner.Api.Data;
using FileScanner.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Database
var dbPath = Path.Combine(builder.Environment.ContentRootPath, "filescanner.db");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// JWT Auth
var jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") 
    ?? "FileScannerSuperSecretKey2026!@#$%^&*()";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });
builder.Services.AddAuthorization();

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
    case "9router": // 9Router support
        builder.Services.AddHttpClient<OpenAiCompatibleLlmService>();
        builder.Services.AddSingleton<ILlmService>(sp =>
            new OpenAiCompatibleLlmService(
                llmOptions.Provider == "openai" ? llmOptions.OpenAi
                : llmOptions.Provider == "openrouter" ? llmOptions.OpenRouter
                : llmOptions.Provider == "9router" ? llmOptions.Ninerouter
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

// Database migration
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();