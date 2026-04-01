using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FileScanner.Api.Models;

namespace FileScanner.Api.Services;

/// <summary>
/// OpenAI-compatible LLM provider using HTTP REST API.
/// Supports OpenAI, OpenRouter, and Gemini (via their OpenAI-compatible endpoints).
/// Uses only HttpClient and System.Text.Json — no external SDK dependencies.
/// </summary>
public sealed class OpenAiCompatibleLlmService : ILlmService
{
    private readonly HttpClient _http;
    private readonly string _visionModel;
    private readonly string _textModel;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public OpenAiCompatibleLlmService(OpenAiCompatibleOptions options, HttpClient httpClient)
    {
        _visionModel = options.VisionModel;
        _textModel = options.TextModel;

        _http = httpClient;
        _http.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
        if (!string.IsNullOrEmpty(options.ApiKey))
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", options.ApiKey);
    }

    /// <summary>Sends image bytes to a multimodal model (gpt-4o, gemini, etc.) via REST API.</summary>
    public async Task<(ScanResult Result, double Confidence)> ExtractFromImageAsync(byte[] imageBytes)
    {
        try
        {
            var base64 = Convert.ToBase64String(imageBytes);
            var mime = DetectMimeType(imageBytes);
            var dataUrl = $"data:{mime};base64,{base64}";

            var request = new ChatRequest
            {
                Model = _visionModel,
                Messages =
                [
                    new ChatMessage
                    {
                        Role = "user",
                        Content =
                        [
                            new ContentPart { Type = "text", Text = BuildPrompt() },
                            new ContentPart
                            {
                                Type = "image_url",
                                ImageUrl = new ImageUrl { Url = dataUrl }
                            }
                        ]
                    }
                ]
            };

            var responseText = await PostChatAsync(request);
            return ParseResponse(responseText);
        }
        catch
        {
            return (new ScanResult(), 0.0);
        }
    }

    /// <summary>Sends OCR text to a text model for JSON structuring.</summary>
    public async Task<(ScanResult Result, double Confidence)> ExtractFromTextAsync(string ocrText)
    {
        if (string.IsNullOrWhiteSpace(ocrText))
            return (new ScanResult(), 0.0);

        try
        {
            var request = new ChatRequest
            {
                Model = _textModel,
                Messages =
                [
                    new ChatMessage
                    {
                        Role = "user",
                        Content =
                        [
                            new ContentPart
                            {
                                Type = "text",
                                Text = $"{BuildPrompt()}\n\nOCR Text:\n{ocrText}"
                            }
                        ]
                    }
                ]
            };

            var responseText = await PostChatAsync(request);
            return ParseResponse(responseText);
        }
        catch
        {
            return (new ScanResult(), 0.0);
        }
    }

    private async Task<string> PostChatAsync(ChatRequest request)
    {
        var json = JsonSerializer.Serialize(request, JsonOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var response = await _http.PostAsync("chat/completions", content);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? string.Empty;
    }

    /// <summary>Detects MIME type from magic bytes. Falls back to JPEG if unknown.</summary>
    private static string DetectMimeType(byte[] bytes)
    {
        if (bytes.Length >= 4 && bytes[0] == 0x89 && bytes[1] == 0x50) return "image/png";
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xD8) return "image/jpeg";
        if (bytes.Length >= 4 && bytes[0] == 0x52 && bytes[1] == 0x49) return "image/webp";
        return "image/jpeg"; // safe default
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

    // ─── Private request/response model types (local to this file) ────────────────────

    private sealed class ChatRequest
    {
        public string Model { get; set; } = string.Empty;
        public List<ChatMessage> Messages { get; set; } = [];
    }

    private sealed class ChatMessage
    {
        public string Role { get; set; } = string.Empty;
        public List<ContentPart> Content { get; set; } = [];
    }

    private sealed class ContentPart
    {
        public string Type { get; set; } = string.Empty;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Text { get; set; }

        [JsonPropertyName("image_url")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ImageUrl? ImageUrl { get; set; }
    }

    private sealed class ImageUrl
    {
        public string Url { get; set; } = string.Empty;
    }
}
