using Microsoft.Extensions.Logging;

namespace ProductsApplicationLayer;

public class CorpusIndexer : ICorpusIndexer
{
    private readonly IProductService _productService;
    private readonly IVectorSearchService _vectorService;
    private readonly ILogger<CorpusIndexer> _logger;

    public CorpusIndexer(IProductService productService, IVectorSearchService vectorService,
        ILogger<CorpusIndexer> logger)
    {
        _productService = productService;
        _vectorService = vectorService;
        _logger = logger;
    }
    public async Task RunFullIndexingAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting indexing of product corpus...");
        await _vectorService.EnsureCollectionExistsAsync(forceRecreate: true);

        var products = _productService.GetAllProducts().ToList();
    
        // We use Parallel.ForEachAsync to run 3-4 batches at once
        // This keeps your GPU and Disk I/O busy
        var options = new ParallelOptions 
        { 
            MaxDegreeOfParallelism = 3, // Adjust this based on your 3080 VRAM
            CancellationToken = ct 
        };

        await Parallel.ForEachAsync(products.Chunk(100), options, async (batch, token) =>
        {
            try
            {
                await _vectorService.UpsertBatchAsync(batch.ToList());
                _logger.LogInformation("Indexed a batch of {Count} items...", batch.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to index a batch.");
            }
        });

        _logger.LogInformation("Corpus indexing completed successfully.");
    }
    
    public async Task RunPricePatchAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting PAYLOAD ONLY patch (no new embeddings)...");
    
        // IMPORTANT: We do NOT recreate the collection here.
        // We just want to update the existing data.

        var products = _productService.GetAllProducts().ToList();
        const int batchSize = 100;

        for (int i = 0; i < products.Count; i += batchSize) 
        {
            if (ct.IsCancellationRequested) break;
    
            var batch = products.Skip(i).Take(batchSize).ToList();
            try
            {
                // Call the patch function that updates only Title, Category, and Price
                await _vectorService.PatchPayloadBatchAsync(batch);
            
                _logger.LogInformation("Patched payloads {Count}/{Total}...", i + batch.Count, products.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to patch batch starting at {Index}", i);
            } 
        }
        _logger.LogInformation("Payload patching completed successfully.");
    }
}