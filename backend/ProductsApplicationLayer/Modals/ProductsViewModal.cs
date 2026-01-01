namespace ProductsApplicationLayer.ViewModals;

public class ProductViewModal
{
    // CsvHelper will use this empty constructor
    public ProductViewModal() { }

    public string? Category { get; set; }
    public string? Title { get; set; }
    public decimal Price { get; set; }
    public string? Grammage { get; set; }
    public string? VectorId { get; set; }
    public float Score { get; set; }
}