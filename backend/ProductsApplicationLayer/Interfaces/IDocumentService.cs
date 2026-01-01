namespace ProductsApplicationLayer;

public interface IDocumentService
{
    Task<List<string>> ExtractReceiptData(Stream file, string fileName);
}