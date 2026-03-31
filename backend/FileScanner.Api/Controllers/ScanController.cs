using Microsoft.AspNetCore.Mvc;
using FileScanner.Api.Models;
using FileScanner.Api.Services;

namespace FileScanner.Api.Controllers;

[ApiController]
[Route("api")]
public class ScanController : ControllerBase
{
    private readonly ExtractionOrchestrator _orchestrator;
    private readonly PdfService _pdf;
    private static readonly HashSet<string> AllowedExtensions = [".png", ".jpg", ".jpeg", ".webp", ".pdf"];
    private const long MaxFileSizeBytes = 20 * 1024 * 1024; // 20MB

    public ScanController(ExtractionOrchestrator orchestrator, PdfService pdf)
    {
        _orchestrator = orchestrator;
        _pdf = pdf;
    }

    [HttpGet("health")]
    public IActionResult Health() => Ok(new { status = "ok" });

    [HttpPost("scan")]
    public async Task<ActionResult<ScanResult>> Scan(IFormFile file)
    {
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

        ScanResult result;
        if (ext == ".pdf")
        {
            var pages = _pdf.ExtractPageImages(fileBytes);
            if (pages.Count == 0)
                return StatusCode(500, new { error = "PDF'den görüntü çıkarılamadı." });
            result = await _orchestrator.ProcessMultiPageAsync(pages);
        }
        else
        {
            result = await _orchestrator.ProcessImageAsync(fileBytes);
        }

        return Ok(result);
    }
}
