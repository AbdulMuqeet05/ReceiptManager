namespace ProductsApplicationLayer.ViewModals;

public class ProcessedReceiptItem
{
    // Data from the OCR (Ollama Vision)
    public string OriginalName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public double UnitPrice { get; set; }
    public double TotalPrice { get; set; }

    // Data from Qdrant Vector Search
    public string MatchedName { get; set; } = "Unknown Product";
    public string MatchedCategory { get; set; } = "N/A";
    
    // Pro-Tip: Add a Match Confidence or Price Validation
    public double? DatabasePrice { get; set; }
    
    // You can calculate if the price on the receipt matches your database
    public bool IsPriceValid { get; set; }
    public float MatchConfidence { get; set; }
}