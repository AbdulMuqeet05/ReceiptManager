using Microsoft.AspNetCore.Mvc;
using ProductsApplicationLayer;

namespace WebApplication1.Controllers;

public class AdminController : ControllerBase
{
    
    private readonly ICorpusIndexer _indexer;
    private readonly ILogger<AdminController> _logger;

    public AdminController(ICorpusIndexer indexer, ILogger<AdminController> logger)
    {
        _indexer = indexer;
        _logger = logger;
    }
    [HttpPost("index-corpus")]
    public IActionResult Index([FromServices] IServiceScopeFactory scopeFactory)
    {
        _ = Task.Run(async () =>
        {
            using var scope = scopeFactory.CreateScope();
            // Resolve a NEW indexer inside this scope
            var indexer = scope.ServiceProvider.GetRequiredService<ICorpusIndexer>(); 
        
            try
            {
                await indexer.RunFullIndexingAsync();
            }
            catch (Exception ex)
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "Background indexing failed.");
            }
        });

        return Accepted(new { message = "Indexing started. Monitor logs." });
    }
    
    [HttpPost("patch-corpus-price")]
    public IActionResult PatchCorpus([FromServices] IServiceScopeFactory scopeFactory)
    {
        _ = Task.Run(async () =>
        {
            using var scope = scopeFactory.CreateScope();
            // Resolve a NEW indexer inside this scope
            var indexer = scope.ServiceProvider.GetRequiredService<ICorpusIndexer>(); 
        
            try
            {
                await indexer.RunPricePatchAsync();
            }
            catch (Exception ex)
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "Background indexing failed.");
            }
        });

        return Accepted(new { message = "Indexing started. Monitor logs." });
    }
}