using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace FileScanner.Api.Services;

public class PdfService
{
    // PDF'den hem metin hem görsel çıkaran service
    // 1. Önce metin katmanı var mı kontrol et
    // 2. Varsa metni çıkart (dijital PDF için)
    // 3. Yoksa görsel çıkarmayı dene (taranmış PDF için)
    
    public (List<byte[]> images, string? text) ExtractPageImages(byte[] pdfBytes)
    {
        var images = new List<byte[]>();
        string? extractedText = null;
        
        try
        {
            using var document = PdfDocument.Open(pdfBytes);
            var pageCount = document.NumberOfPages;
            
            if (pageCount == 0)
            {
                Console.WriteLine("[PdfService] PDF has no pages");
                return (images, null);
            }
            
            Console.WriteLine($"[PdfService] Processing PDF with {pageCount} pages");
            
            // Önce metin katmanını kontrol et
            var hasText = false;
            var allText = new List<string>();
            
            foreach (var page in document.GetPages())
            {
                // Metin var mı kontrol et
                var pageText = page.Text;
                if (!string.IsNullOrWhiteSpace(pageText) && pageText.Length > 10)
                {
                    hasText = true;
                    allText.Add(pageText);
                }
                
                // Görsel extraction dene (fallback için)
                foreach (var image in page.GetImages())
                {
                    try
                    {
                        var bytes = image.RawBytes.ToArray();
                        if (bytes.Length > 100)
                        {
                            images.Add(bytes);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[PdfService] Error extracting image: {ex.Message}");
                    }
                }
            }
            
            // Metin varsa kullan
            if (hasText)
            {
                extractedText = string.Join("\n\n--- Page Break ---\n\n", allText);
                Console.WriteLine($"[PdfService] Extracted text: {extractedText.Length} characters from {pageCount} pages");
            }
            
            Console.WriteLine($"[PdfService] Total images extracted: {images.Count}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PdfService] PDF parsing error: {ex.Message}");
        }
        
        return (images, extractedText);
    }
}
