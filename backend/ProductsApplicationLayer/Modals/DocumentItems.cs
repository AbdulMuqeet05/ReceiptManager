using System.Text.Json.Serialization;

namespace ProductsApplicationLayer.ViewModals;

public class DocumentItems
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("stk")]
    public int Quantity { get; set; } = 1;

    [JsonPropertyName("unit_price")]
    public double UnitPrice { get; set; } = 0.0;

    [JsonPropertyName("total_price")]
    public double TotalPrice { get; set; } = 0.0;

    [JsonPropertyName("pfand")]
    public double Pfand { get; set; } = 0.00;

}

public class OllamaReceiptRoot
{
    [JsonPropertyName("items")]
    public List<DocumentItems> Items { get; set; } = new();
    [JsonPropertyName("grand_total")]
    public decimal GrandTotal { get; set; }
    [JsonPropertyName("currency")]
    public string Currency { get; set; }
}
