using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using FileScanner.Api.Data;
using FileScanner.Api.Models;
using FileScanner.Api.Services;

namespace FileScanner.Api.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class ScanController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly PdfService _pdf;
    private readonly IHttpClientFactory _httpFactory;
    private static readonly HashSet<string> AllowedExtensions = [".png", ".jpg", ".jpeg", ".webp", ".pdf"];
    private const long MaxFileSizeBytes = 20 * 1024 * 1024; // 20MB

    public ScanController(AppDbContext db, PdfService pdf, IHttpClientFactory httpFactory)
    {
        _db = db;
        _pdf = pdf;
        _httpFactory = httpFactory;
    }

    [HttpGet("health")]
    [AllowAnonymous]
    public IActionResult Health() => Ok(new { status = "ok" });

    [HttpPost("scan")]
    public async Task<ActionResult<ScanResult>> Scan(IFormFile file)
    {
        var userId = GetUserId();
        var user = await _db.Users.FindAsync(userId);
        if (user == null || string.IsNullOrEmpty(user.ApiKey))
            return Unauthorized(new { error = "API key ayarlanmamış. Lütfen önce API key ekleyin." });

        if (file == null || file.Length == 0)
            return BadRequest(new { error = "Dosya yüklenmedi." });

        if (file.Length > MaxFileSizeBytes)
            return BadRequest(new { error = "Dosya boyutu 20MB sınırını aşıyor." });

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
            return BadRequest(new { error = $"Desteklenmeyen format: {ext}. PNG, JPG, JPEG, WEBP veya PDF yükleyin." });

        using var ms = new MemoryStream();
        await file.CopyToAsync(ms);
        var fileBytes = ms.ToArray();

        // Get user's API key (decrypted)
        var apiKey = AuthController.Decrypt(user.ApiKey);
        
        // Create LLM service with user's API key
        var llmService = new OpenAiCompatibleLlmService(
            new OpenAiCompatibleOptions
            {
                ApiKey = apiKey,
                BaseUrl = "https://9router.mezabilisim.com/v1",
                VisionModel = "my-combo",
                TextModel = "my-combo"
            },
            _httpFactory.CreateClient(nameof(OpenAiCompatibleLlmService))
        );

        // Create services for this request
        var ocrService = new OcrService(null!);
        var orchestrator = new ExtractionOrchestrator(ocrService, llmService);

        ScanResult result;
        if (ext == ".pdf")
        {
            var pages = _pdf.ExtractPageImages(fileBytes);
            if (pages.Count == 0)
                return StatusCode(500, new { error = "PDF'den görüntü çıkarılamadı." });
            result = await orchestrator.ProcessMultiPageAsync(pages);
        }
        else
        {
            result = await orchestrator.ProcessImageAsync(fileBytes);
        }

        return Ok(result);
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier);
        return int.Parse(claim?.Value ?? "0");
    }
}