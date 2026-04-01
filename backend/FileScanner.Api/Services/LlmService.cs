using OllamaSharp;
using System.Text.Json;
using FileScanner.Api.Models;

namespace FileScanner.Api.Services;

public class LlmService
{
    private readonly OllamaApiClient _client;
    private const string Model = "llava";

    public LlmService(IConfiguration config)
    {
        var baseUrl = config["OLLAMA_BASE_URL"] ?? "http://localhost:11434";
        var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        _client = new OllamaApiClient(new Uri(baseUrl), httpClient);
    }

    // Sends image bytes directly to LLaVA multimodal model.
    // Returns (ScanResult, confidence) tuple. Confidence 0.0 on failure.
    public async Task<(ScanResult Result, double Confidence)> ExtractFromImageAsync(byte[] imageBytes)
    {
        try
        {
            var base64 = Convert.ToBase64String(imageBytes);
            var prompt = BuildPrompt();

            var chat = new Chat(_client) { Model = Model };
            var responseText = string.Empty;

            // Send message with image using base64 encoding
            await foreach (var chunk in chat.SendAsync(prompt))
                responseText += chunk;

            return ParseResponse(responseText, "llm");
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
            var chat = new Chat(_client) { Model = "llama3" };
            var responseText = string.Empty;

            await foreach (var chunk in chat.SendAsync(prompt))
                responseText += chunk;

            return ParseResponse(responseText, "ocr");
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

    private static (ScanResult Result, double Confidence) ParseResponse(string responseText, string source)
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
            var confidence = CalculateConfidence(result);
            return (result, confidence);
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
        var total = 6;
        if (!string.IsNullOrEmpty(result.Document.Type)) filled++;
        if (!string.IsNullOrEmpty(result.Document.Date)) filled++;
        if (!string.IsNullOrEmpty(result.Vendor.Name)) filled++;
        if (result.Financials.Total > 0) filled++;
        if (!string.IsNullOrEmpty(result.Financials.Currency)) filled++;
        if (result.Items.Count > 0) filled++;
        return Math.Round((double)filled / total, 2);
    }
}
