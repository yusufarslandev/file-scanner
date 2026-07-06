namespace FileScanner.Api.Services;

/// <summary>Root configuration for LLM provider selection and settings.</summary>
public sealed class LlmOptions
{
    /// <summary>Provider name: "ollama", "openai", "openrouter", "9router", or "gemini".</summary>
    public string Provider { get; set; } = "ollama";

    public OllamaOptions Ollama { get; set; } = new();
    public OpenAiCompatibleOptions OpenAi { get; set; } = new();
    public OpenAiCompatibleOptions OpenRouter { get; set; } = new();
    public OpenAiCompatibleOptions Ninerouter { get; set; } = new();
    public OpenAiCompatibleOptions Gemini { get; set; } = new();
}

/// <summary>Configuration for Ollama (local) provider.</summary>
public sealed class OllamaOptions
{
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public string VisionModel { get; set; } = "llava";
    public string TextModel { get; set; } = "llama3";
}

/// <summary>Configuration for OpenAI, OpenRouter, and Gemini (OpenAI-compatible API endpoint).</summary>
public sealed class OpenAiCompatibleOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public string VisionModel { get; set; } = string.Empty;
    public string TextModel { get; set; } = string.Empty;
}
