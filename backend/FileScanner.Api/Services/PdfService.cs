using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace FileScanner.Api.Services;

public class PdfService
{
    // Converts each PDF page to a PNG byte array using PdfPig + SkiaSharp rendering.
    // PdfPig does not render to bitmap natively; we extract page images embedded in the PDF.
    // For pages without embedded images, we return an empty array (will yield low confidence).
    public List<byte[]> ExtractPageImages(byte[] pdfBytes)
    {
        var results = new List<byte[]>();
        using var document = PdfDocument.Open(pdfBytes);
        foreach (var page in document.GetPages())
        {
            foreach (var image in page.GetImages())
            {
                try
                {
                    var bytes = image.RawBytes.ToArray();
                    if (bytes.Length > 0)
                        results.Add(bytes);
                }
                catch
                {
                    // skip unreadable embedded images
                }
            }
        }
        return results;
    }
}
