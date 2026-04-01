using System.Diagnostics;
using FileScanner.Api.Models;

namespace FileScanner.Api.Services;

public class ExtractionOrchestrator
{
    private readonly OcrService _ocr;
    private readonly ILlmService _llm;

    public ExtractionOrchestrator(OcrService ocr, ILlmService llm)
    {
        _ocr = ocr;
        _llm = llm;
    }

    // For vision-capable providers: runs OCR and vision LLM in parallel, uses vision result.
    // For text-only providers: runs OCR, then feeds text to LLM for structuring.
    public async Task<ScanResult> ProcessImageAsync(byte[] imageBytes)
    {
        var sw = Stopwatch.StartNew();
        var ocrTextTask = Task.Run(() => _ocr.ExtractText(imageBytes));

        if (_llm.SupportsVision)
        {
            // Cloud / multimodal providers (OpenAI, Gemini, etc.): single API call to process image directly
            var llmImageTask = _llm.ExtractFromImageAsync(imageBytes);
            await Task.WhenAll(ocrTextTask, llmImageTask);
            sw.Stop();

            var result = llmImageTask.Result.Result;
            result.OcrText = ocrTextTask.Result;
            result.Meta = new MetaInfo
            {
                Confidence = llmImageTask.Result.Confidence,
                Source = ExtractionSource.Llm,
                ProcessingTimeMs = sw.ElapsedMilliseconds
            };
            return result;
        }
        else
        {
            // Text-only or local fallback: OCR → LLM text structuring
            var ocrText = await ocrTextTask;
            var llmTextResult = await _llm.ExtractFromTextAsync(ocrText);
            sw.Stop();

            llmTextResult.Result.OcrText = ocrText;
            llmTextResult.Result.Meta = new MetaInfo
            {
                Confidence = llmTextResult.Confidence,
                Source = ExtractionSource.Ocr,
                ProcessingTimeMs = sw.ElapsedMilliseconds
            };
            return llmTextResult.Result;
        }
    }

    // For multi-page PDFs: process each page and merge results (first page wins for document/vendor/financials, items merged).
    public async Task<ScanResult> ProcessMultiPageAsync(List<byte[]> pages)
    {
        if (pages.Count == 0) return new ScanResult();
        if (pages.Count == 1) return await ProcessImageAsync(pages[0]);

        var tasks = pages.Select(ProcessImageAsync).ToList();
        var results = await Task.WhenAll(tasks);

        var primary = results[0];
        foreach (var subsequent in results.Skip(1))
            primary.Items.AddRange(subsequent.Items);

        return primary;
    }
}
