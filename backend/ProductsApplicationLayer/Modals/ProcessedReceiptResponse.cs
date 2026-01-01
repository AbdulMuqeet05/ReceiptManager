namespace ProductsApplicationLayer.ViewModals;

public class ProcessedReceiptResponse
{
    public List<ProcessedReceiptItem> Items { get; set; } = new();
    public decimal ReceiptGrandTotal { get; set; }
    public string Currency { get; set; } = "EUR";
    
    // Logic to see if the sum of items matches the grand total
    public double CalculatedTotal => Items.Sum(x => x.TotalPrice);
}