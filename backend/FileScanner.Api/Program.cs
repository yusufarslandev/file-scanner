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
builder.Services.AddSingleton<LlmService>();
builder.Services.AddSingleton<ExtractionOrchestrator>();

var app = builder.Build();
app.UseCors();
app.MapControllers();
app.Run();
