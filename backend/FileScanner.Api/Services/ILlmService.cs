using FileScanner.Api.Models;

namespace FileScanner.Api.Services;

/// <summary>
/// Abstraction for LLM-based data extraction. Implementations can use Ollama, OpenAI, OpenRouter, or Gemini.
/// </summary>
public interface ILlmService
{
    /// <summary>True if provider supports direct image processing (vision models). False for text-only providers.</summary>
    bool SupportsVision { get; }

    /// <summary>Sends raw image bytes to a multimodal model. Returns (ScanResult, confidence).</summary>
    Task<(ScanResult Result, double Confidence)> ExtractFromImageAsync(byte[] imageBytes);

    /// <summary>Sends OCR-extracted text to a text model for JSON structuring.</summary>
    Task<(ScanResult Result, double Confidence)> ExtractFromTextAsync(string ocrText);
}
