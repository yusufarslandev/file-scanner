using OllamaSharp;
using System.Text.Json;
using FileScanner.Api.Models;

namespace FileScanner.Api.Services;

/// <summary>
/// Ollama-based LLM provider (local).
/// Uses OllamaSharp to communicate with a local Ollama server.
/// Fixes the critical bug where image bytes were silently dropped.
/// </summary>
public sealed class OllamaLlmService : ILlmService
{
    private readonly OllamaApiClient _client;
    private readonly string _visionModel;
    private readonly string _textModel;

    public bool SupportsVision => true;

    public OllamaLlmService(LlmOptions options)
    {
        var cfg = options.Ollama;
        _client = new OllamaApiClient(cfg.BaseUrl);
        _visionModel = cfg.VisionModel;
        _textModel = cfg.TextModel;
    }

    // Sends image bytes directly to LLaVA multimodal model using OllamaSharp's image overload.
    // BUG FIX: The original code encoded to base64 but passed null (no images) to SendAsync.
    // Now we pass [imageBytes] explicitly to the SendAsync(string, IEnumerable<byte[]>) overload.
    public async Task<(ScanResult Result, double Confidence)> ExtractFromImageAsync(byte[] imageBytes)
    {
        try
        {
            var chat = new Chat(_client) { Model = _visionModel };
            var responseText = string.Empty;

            // OllamaSharp's SendAsync(string, IEnumerable<byte[]>) overload sends images as base64 internally
            await foreach (var chunk in chat.SendAsync(BuildPrompt(), [imageBytes]))
                responseText += chunk;

            return ParseResponse(responseText);
        }
        catch
        {
            return (new ScanResult(), 0.0);
        }
    }

    // Sends OCR-extracted text to Ollama for JSON structuring.
    public async Task<(ScanResult Result, double Confidence)> ExtractFromTextAsync(string ocrText)
    {
        if (string.IsNullOrWhiteSpace(ocrText))
            return (new ScanResult(), 0.0);

        try
        {
            var prompt = $"{BuildPrompt()}\n\nOCR Text:\n{ocrText}";
            var chat = new Chat(_client) { Model = _textModel };
            var responseText = string.Empty;

            await foreach (var chunk in chat.SendAsync(prompt))
                responseText += chunk;

            return ParseResponse(responseText);
        }
        catch
        {
            return (new ScanResult(), 0.0);
        }
    }

    private static string BuildPrompt() => """
        You are a receipt and invoice data extraction assistant.
        Extract ALL fields from the document and return ONLY valid JSON matching this exact schema:
        {
          "document": { "type": "", "date": "", "invoiceNo": "" },
          "vendor": { "name": "", "taxNo": "", "address": "", "phone": "", "email": "" },
          "financials": { "subtotal": 0, "vat": 0, "total": 0, "currency": "TRY", "paymentMethod": "" },
          "items": [{ "name": "", "quantity": 0, "unitPrice": 0, "lineTotal": 0 }]
        }
        Return ONLY the JSON object. No explanation, no markdown.
        """;

    private static (ScanResult Result, double Confidence) ParseResponse(string responseText)
    {
        try
        {
            // Extract JSON from response (strip any markdown code fences)
            var json = responseText.Trim();
            var start = json.IndexOf('{');
            var end = json.LastIndexOf('}');
            if (start < 0 || end < 0) return (new ScanResult(), 0.0);
            json = json[start..(end + 1)];

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = JsonSerializer.Deserialize<ScanResult>(json, options) ?? new ScanResult();
            return (result, CalculateConfidence(result));
        }
        catch
        {
            return (new ScanResult(), 0.0);
        }
    }

    // Confidence = ratio of non-empty required fields
    private static double CalculateConfidence(ScanResult result)
    {
        var filled = 0;
        const int total = 6;
        if (!string.IsNullOrEmpty(result.Document.Type)) filled++;
        if (!string.IsNullOrEmpty(result.Document.Date)) filled++;
        if (!string.IsNullOrEmpty(result.Vendor.Name)) filled++;
        if (result.Financials.Total > 0) filled++;
        if (!string.IsNullOrEmpty(result.Financials.Currency)) filled++;
        if (result.Items.Count > 0) filled++;
        return Math.Round((double)filled / total, 2);
    }
}
