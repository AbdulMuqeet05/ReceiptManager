using FuzzySharp;
using Microsoft.Extensions.Logging;
using ProductsApplicationLayer.ViewModals;

namespace ProductsApplicationLayer;

public class ReceiptProcessor: IReceiptProcessor
{
    
    private readonly IVectorSearchService _vectorService;
    private readonly ILogger<ReceiptProcessor> _logger;
    private readonly IProductService _productService;
    public ReceiptProcessor(IVectorSearchService vectorService, ILogger<ReceiptProcessor> logger, IProductService productService)
    {
        _vectorService = vectorService;
        _logger = logger;
        _productService = productService;
    }
    
    public async Task<List<ProcessedReceiptItem>> ProcessAndMatchReceiptAsync(OllamaReceiptRoot rawReceipt)
    {
        var matchedItems = new List<ProcessedReceiptItem>();
        var categories = _productService.GetAllProducts().Select(c=>c.Category).Distinct().ToList();
        
        foreach (var item in rawReceipt.Items)
        {
            var dbMatch = await _vectorService.SearchSimilarProductAsync(item.Name);

            matchedItems.Add(new ProcessedReceiptItem
            {
                OriginalName = item.Name,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.TotalPrice,

                // If dbMatch is null (due to low score), these will be "Unknown"
                MatchedName = dbMatch?.Title ?? "Unknown Product",
                MatchedCategory = dbMatch?.Category ?? "N/A",
            
                // Map the patched price and the similarity score
                DatabasePrice = (double?)dbMatch?.Price,
                MatchConfidence = dbMatch?.Score ?? 0,
            
                // IsPriceValid: True only if we found a match AND prices are similar
                IsPriceValid = dbMatch != null && 
                               Math.Abs((double)item.UnitPrice - (double)(dbMatch?.Price ?? 0)) < 0.50
            });
        }

        return matchedItems;
    }

    public async Task<ProcessedReceiptResponse> MatchReceiptItems(OllamaReceiptRoot rawReceipt)
    {
        var response = new ProcessedReceiptResponse
        {
            ReceiptGrandTotal = (decimal)rawReceipt.GrandTotal,
            Currency = rawReceipt.Currency ?? "EUR"
        };

        foreach (var item in rawReceipt.Items)
        {
            // STEP 1: Recall - Get Top 20 Semantic Candidates
            // We fetch 20 instead of 1 so we can re-rank them using Price and Brand info
            var vectorCandidates = await _vectorService.SearchAsync(item.Name, 20);

            if (!vectorCandidates.Any()) continue;

            // STEP 2: Precision - Re-Ranking with Price and Fuzzy Logic
            var reRanked = vectorCandidates.Select(c =>
            {
                var dbName = c.Payload["full_name"].StringValue;
                var dbPrice = c.Payload["price"].DoubleValue;
                
                // A. Fuzzy Score (Brand/Name overlap)
                double fuzzyScore = Fuzz.WeightedRatio(item.Name.ToLower(), dbName.ToLower()) / 100.0;

                // B. Price Similarity Score (1.0 = exact match, decreases as difference grows)
                // Example: If diff is 0.50€, score is ~0.66. If diff is 0€, score is 1.0.
                double priceDiff = Math.Abs(dbPrice - item.UnitPrice);
                double priceScore = 1.0 / (1.0 + priceDiff);

                // C. Final Weighted Formula
                // We give Price a strong vote to distinguish between brands
                double finalScore = (c.Score * 0.4) + (fuzzyScore * 0.4) + (priceScore * 0.2);

                return new
                {
                    Candidate = c,
                    DbName = dbName,
                    DbPrice = dbPrice,
                    DbCategory = c.Payload["category"].StringValue,
                    Score = finalScore
                };
            })
            .OrderByDescending(r => r.Score)
            .First();

            // STEP 3: Map to your Clean ViewModal
            response.Items.Add(new ProcessedReceiptItem
            {
                OriginalName = item.Name,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                TotalPrice = item.TotalPrice,

                MatchedName = reRanked.DbName,
                MatchedCategory = reRanked.DbCategory,
                DatabasePrice = reRanked.DbPrice,
                
                // Price is valid if difference is less than 5 cents (handles slight rounding)
                IsPriceValid = Math.Abs(reRanked.DbPrice - item.UnitPrice) < 0.05,
                MatchConfidence = (float)reRanked.Score
            });
        }

        return response;
    }
}