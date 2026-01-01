namespace ProductsApplicationLayer;

public interface ICorpusIndexer
{
    Task RunFullIndexingAsync(CancellationToken ct = default);
    Task RunPricePatchAsync(CancellationToken ct = default);
}