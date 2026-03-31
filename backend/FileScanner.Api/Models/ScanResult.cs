namespace FileScanner.Api.Models;

public class ScanResult
{
    public DocumentInfo Document { get; set; } = new();
    public VendorInfo Vendor { get; set; } = new();
    public FinancialsInfo Financials { get; set; } = new();
    public List<LineItem> Items { get; set; } = [];
    public MetaInfo Meta { get; set; } = new();
}

public class DocumentInfo
{
    public string Type { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string InvoiceNo { get; set; } = string.Empty;
}

public class VendorInfo
{
    public string Name { get; set; } = string.Empty;
    public string TaxNo { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class FinancialsInfo
{
    public decimal Subtotal { get; set; }
    public decimal Vat { get; set; }
    public decimal Total { get; set; }
    public string Currency { get; set; } = "TRY";
    public string PaymentMethod { get; set; } = string.Empty;
}

public class LineItem
{
    public string Name { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}

public class MetaInfo
{
    public double Confidence { get; set; }
    public ExtractionSource Source { get; set; } = ExtractionSource.Ocr;
    public long ProcessingTimeMs { get; set; }
}
