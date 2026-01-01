using ProductsApplicationLayer.ViewModals;
using Qdrant.Client.Grpc;

namespace ProductsApplicationLayer;

public interface IVectorSearchService
{
    public Task UpsertBatchAsync(List<ProductViewModal> products);
    public Task PatchPayloadBatchAsync(List<ProductViewModal> products);
    public Task<ProductViewModal?> SearchSimilarProductAsync(string receiptItemName);
    public Task<IEnumerable<ScoredPoint>?> SearchAsync(string query, ulong limit);
    public Task<IEnumerable<ScoredPoint>?> getSimilarProductsToptwenty(string query, double priceFilter, string category);
    public Task EnsureCollectionExistsAsync(bool forceRecreate = false);

}