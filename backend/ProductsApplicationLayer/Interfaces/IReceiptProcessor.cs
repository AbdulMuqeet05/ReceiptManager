using ProductsApplicationLayer.ViewModals;

namespace ProductsApplicationLayer;

public interface IReceiptProcessor
{
    public Task<List<ProcessedReceiptItem>> ProcessAndMatchReceiptAsync(OllamaReceiptRoot rawReceipt);
    public Task<ProcessedReceiptResponse> MatchReceiptItems(OllamaReceiptRoot rawReceipt);
}