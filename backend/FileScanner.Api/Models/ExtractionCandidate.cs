namespace FileScanner.Api.Models;

public class ExtractionCandidate
{
    public ScanResult Result { get; set; } = new();
    public double Confidence { get; set; }
    public ExtractionSource Source { get; set; } = ExtractionSource.Ocr;
}
