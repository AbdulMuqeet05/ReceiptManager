using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Google.Protobuf.Collections;
using Microsoft.Extensions.Logging;
using ProductsApplicationLayer;
using ProductsApplicationLayer.ViewModals;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using Range = Qdrant.Client.Grpc.Range;

namespace Infrastructure.Qdrant;

public class QdrantVectorService :IVectorSearchService
{
    private readonly QdrantClient _qdrantClient;
    private readonly IDocumentAnalyzer _ollama;
    private const string CollectionName = "products";
    private readonly ILogger<QdrantVectorService> _logger;

    public QdrantVectorService(QdrantClient qdrantClient, IDocumentAnalyzer ollama, ILogger<QdrantVectorService> logger)
    {
        _qdrantClient = qdrantClient;
        _ollama = ollama;
        _logger = logger;
    }

    public async Task<IEnumerable<ScoredPoint>?> SearchAsync(string query, ulong limit)
    {
        var queryVector = await _ollama.GetEmbeddingAsync(query);
        var searchResult = await _qdrantClient.SearchAsync(CollectionName, queryVector, limit: limit);
        
        
        var hits = searchResult.ToList().Take(100);

        // LOG THE SCORE for debugging
        if (hits != null)
        {
            _logger.LogInformation("Match for '{Name}'", 
                query);
        }
        
        return hits;
    }

    public async Task<ProductViewModal?> SearchSimilarProductAsync(string receiptItemName)
    {
        
        var keyWords = receiptItemName.Split(' ')
            .Where(x=> x.Length > 2)
            .Select(x=> x.Replace(".", ""))
            .ToList();
        var queryVector = await _ollama.GetEmbeddingAsync(receiptItemName);
        
        // create filter text 
        var filter = new Filter();
        if (keyWords.Any())
        {
            foreach (var keyword in keyWords)
            {
                filter.Should.Add(new Condition { 
                    Field = new FieldCondition { 
                        Key = "full_name", 
                        Match = new Match { Text = keyword }
                    } 
                });
            }
            
        }
    
        // Ask for the top match
        var searchResult = await _qdrantClient.SearchAsync(CollectionName, queryVector, filter:filter, limit: 1);
        var hit = searchResult.FirstOrDefault();

        // LOG THE SCORE for debugging
        if (hit != null)
        {
            _logger.LogInformation("Match for '{Name}': {Match} with Score: {Score}", 
                receiptItemName, hit.Payload["full_name"].StringValue, hit.Score);
        }

        // THRESHOLD GUARD: If score < 0.82, it's a hallucination/bad match.
        if (hit == null || hit.Score < 0.82f) 
        {
            return null; 
        }

        return new ProductViewModal
        {
            Title = hit.Payload["full_name"].StringValue,
            Category = hit.Payload["category"].StringValue,
            // Carry the score through so we can see it in the final JSON
            Score = hit.Score,
            Price = (decimal)(hit.Payload.TryGetValue("price", out var p) ? p.DoubleValue : 0)
        };
    }

    public async Task<IEnumerable<ScoredPoint>?> getSimilarProductsToptwenty(string productName, double priceFilter,
        string category)
    {
        var queryVector = await _ollama.GetEmbeddingAsync(productName);
        var margin = 0.50;
        var minPrice = priceFilter - margin;
        var maxPrice = priceFilter + margin;
        var filter = new Filter();
        filter.Must.Add(new Condition { 
            Field = new FieldCondition { 
               Key = "price",
               Range = new Range
               {
                   Gte = minPrice,
                   Lte = maxPrice
               },
             
            } 
        });
        filter.Must.Add(new Condition
        {
            Field = new FieldCondition
            {
                Key = "category",
                Match = new Match
                {
                    Text = category// Searches for this word within the category string
                }
            }
        });
        var similarProducts = await _qdrantClient.SearchAsync(CollectionName, queryVector, filter, limit: 20);
        return similarProducts.Take(20).ToList();
    }


    public async Task EnsureCollectionExistsAsync(bool forceRecreate = false)
    {
        var collections = await _qdrantClient.ListCollectionsAsync();
        bool exists = collections.Contains(CollectionName);

        if (exists && forceRecreate)
        {
            await _qdrantClient.DeleteCollectionAsync(CollectionName);
            exists = false;
        }

        if (!exists)
        {
            await _qdrantClient.CreateCollectionAsync(CollectionName, 
                new VectorParams { Size = 1024, Distance = Distance.Cosine });
        }
        await _qdrantClient.CreatePayloadIndexAsync(
            collectionName: CollectionName, 
            fieldName: "full_name", 
            schemaType: PayloadSchemaType.Text
        );
    }
    public async Task UpsertBatchAsync(List<ProductViewModal> products)
    {
        // 1. Start all 100 embedding tasks at the SAME TIME
        // This allows the RTX 3080 to process them in a queue much faster
        var tasks = products.Select(async product =>
        {
            try
            {
                var vector = await _ollama.GetEmbeddingAsync(product.Title);
                var pointId = CreateDeterministicGuid(product.Category + product.VectorId);
            
                // Clean up price parsing
                double.TryParse(product.Price.ToString(), System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double priceValue);

                return new PointStruct
                {
                    Id = pointId,
                    Vectors = vector,
                    Payload =
                    {
                        { "full_name", product.Title },
                        { "price", Math.Round(priceValue, 2) },
                        { "category", product.Category },
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing {product.Title}: {ex.Message}");
                return null; // Handle failed items
            }
        }).ToList();

        // 2. Wait for the whole batch to finish embedding
        var results = await Task.WhenAll(tasks);
        var points = results.Where(p => p != null).ToList();

        // 3. Send the batch to Qdrant
        if (points.Any())
        {
            await _qdrantClient.UpsertAsync(CollectionName, points);
        }
    }
    
    


    public async Task PatchPayloadBatchAsync(List<ProductViewModal> products)
    {

        foreach (var product in products)
        {
            var pointId = CreateDeterministicGuid(product.Category + product.VectorId);
        
            // 1. Clean the string and parse it carefully
            string rawPrice = product.Price.ToString().Replace("\"", "").Trim();
        
            if (double.TryParse(rawPrice, System.Globalization.NumberStyles.Any, 
                    System.Globalization.CultureInfo.InvariantCulture, out double priceValue))
            {
                // Round to 2 decimal places to prevent floating point "noise"
                priceValue = Math.Round(priceValue, 2);
                
                // LOG THIS: If this log looks like 2.49, the fix worked.
                _logger.LogInformation("Product: {Name} | Parsed Price: {Price}", product.Title, priceValue);

                var newPayload = new MapField<string, Value>
                {
                    { "full_name", product.Title ?? "" },
                    { "category", product.Category ?? "" },
                    { "price", priceValue } // SDK handles the conversion to gRPC Value
                };

                await _qdrantClient.SetPayloadAsync(CollectionName, newPayload, pointId);
            }
        }
    }
    private Guid CreateDeterministicGuid(string input)
    {
        using MD5 md5 = MD5.Create();
        byte[] hash = md5.ComputeHash(Encoding.Default.GetBytes(input));
        return new Guid(hash);
    }
}