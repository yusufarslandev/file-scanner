using Tesseract;

namespace FileScanner.Api.Services;

public class OcrService
{
    private readonly string _tessDataPath;

    public OcrService(IWebHostEnvironment env)
    {
        // tessdata folder must exist at project root or be set via env var
        _tessDataPath = Environment.GetEnvironmentVariable("TESSDATA_PREFIX")
            ?? Path.Combine(env.ContentRootPath, "tessdata");
    }

    // Returns extracted plain text from image bytes. Empty string on failure.
    public string ExtractText(byte[] imageBytes)
    {
        try
        {
            using var engine = new TesseractEngine(_tessDataPath, "tur+eng", EngineMode.Default);
            using var ms = new MemoryStream(imageBytes);
            using var pix = Pix.LoadFromMemory(ms.ToArray());
            using var page = engine.Process(pix);
            return page.GetText() ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}
