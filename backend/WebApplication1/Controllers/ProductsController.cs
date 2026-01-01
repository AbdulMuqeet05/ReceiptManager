using Microsoft.AspNetCore.Mvc;
using ProductsApplicationLayer;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ILogger<ProductsController> _logger;
    private readonly IProductService _productService;
    private readonly IVectorSearchService _vectorService;
    
    public ProductsController(ILogger<ProductsController> logger, IProductService productService, IVectorSearchService vectorService)
    {
        _logger = logger;
        _productService = productService;
        _vectorService = vectorService;
    }

    [HttpGet]
    public IActionResult Get(int page = 1, int pageSize = 20)
    {
        // Ensure we don't have negative pages
        if (page < 1) page = 1;
        var results = _productService.GetAllProducts()
            .Skip(page - 1 * pageSize)
            .Take(pageSize)
            .ToList();
        _logger.LogInformation($"Page {page} of {pageSize} products returned");
        return Ok(results);
    }
    [HttpGet("getCategories")]
    public async Task<IActionResult> getCaategories()
    {
        var categories = _productService.GetAllProducts().Select(c=>c.Category).Distinct().ToList();
        
        var similarProduct = _vectorService.getSimilarProductsToptwenty("leberwurst", 1.99, "katzen");
        
        return Ok(new {
            similarProduct= similarProduct.Result.ToList(),
            count = similarProduct.Result.ToList().Count
        });
    }

    [HttpGet("test-search")]
    public async Task<IActionResult> SearchInVector(string text)
    {
        var match = await _vectorService.SearchSimilarProductAsync(text);
        return Ok(match);
    }
    
    
    
   
}