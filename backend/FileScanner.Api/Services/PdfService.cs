using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace FileScanner.Api.Services;

public class PdfService
{
    // Converts each PDF page to image bytes using PdfPig + embedded images.
    // Note: PdfPig only extracts images EMBEDDED in the PDF (scanned documents).
    // For vector PDFs or PDFs without images, this returns empty array.
    // In production, consider using pdftoppm/poppler-utils for proper rendering.
    public List<byte[]> ExtractPageImages(byte[] pdfBytes)
    {
        var results = new List<byte[]>();
        
        try
        {
            using var document = PdfDocument.Open(pdfBytes);
            
            // Check if PDF has any pages
            var pageCount = document.NumberOfPages;
            if (pageCount == 0)
            {
                Console.WriteLine("[PdfService] PDF has no pages");
                return results;
            }
            
            Console.WriteLine($"[PdfService] Processing PDF with {pageCount} pages");
            
            foreach (var page in document.GetPages())
            {
                var imageCount = 0;
                foreach (var image in page.GetImages())
                {
                    try
                    {
                        var bytes = image.RawBytes.ToArray();
                        if (bytes.Length > 100) // Minimum 100 bytes to be a valid image
                        {
                            results.Add(bytes);
                            imageCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[PdfService] Error extracting image: {ex.Message}");
                    }
                }
                Console.WriteLine($"[PdfService] Page {page.Number}: {imageCount} images found");
            }
            
            Console.WriteLine($"[PdfService] Total images extracted: {results.Count}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PdfService] PDF parsing error: {ex.Message}");
        }
        
        return results;
    }
}
