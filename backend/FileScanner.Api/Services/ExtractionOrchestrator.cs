using System.Diagnostics;
using FileScanner.Api.Models;

namespace FileScanner.Api.Services;

public class ExtractionOrchestrator
{
    private readonly OcrService _ocr;
    private readonly LlmService _llm;

    public ExtractionOrchestrator(OcrService ocr, LlmService llm)
    {
        _ocr = ocr;
        _llm = llm;
    }

    // Runs OCR→LLM text path and LLaVA image path in parallel.
    // Returns the result with the higher confidence score.
    public async Task<ScanResult> ProcessImageAsync(byte[] imageBytes)
    {
        var sw = Stopwatch.StartNew();

        // Run both paths concurrently
        var ocrTextTask = Task.Run(() => _ocr.ExtractText(imageBytes));
        var llmImageTask = _llm.ExtractFromImageAsync(imageBytes);

        await Task.WhenAll(ocrTextTask, llmImageTask);

        var ocrText = ocrTextTask.Result;
        var llmTextTask = _llm.ExtractFromTextAsync(ocrText);
        await llmTextTask;

        sw.Stop();

        var ocrCandidate = new ExtractionCandidate
        {
            Result = llmTextTask.Result.Result,
            Confidence = llmTextTask.Result.Confidence,
            Source = ExtractionSource.Ocr
        };

        var llmCandidate = new ExtractionCandidate
        {
            Result = llmImageTask.Result.Result,
            Confidence = llmImageTask.Result.Confidence,
            Source = ExtractionSource.Llm
        };

        var best = llmCandidate.Confidence >= ocrCandidate.Confidence ? llmCandidate : ocrCandidate;

        best.Result.Meta = new MetaInfo
        {
            Confidence = best.Confidence,
            Source = best.Source,
            ProcessingTimeMs = sw.ElapsedMilliseconds
        };

        return best.Result;
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
