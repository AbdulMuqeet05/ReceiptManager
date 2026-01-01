using ProductsApplicationLayer.ViewModals;

namespace ProductsApplicationLayer;

public interface IDocumentAnalyzer
{
    Task<OllamaReceiptRoot> AnalyzeAsync(List<string> base64Image);
    public Task<OllamaReceiptRoot> ProcessReceiptWithGroq(List<string> base64Image);

    public Task<float[]> GetEmbeddingAsync(string text);
}