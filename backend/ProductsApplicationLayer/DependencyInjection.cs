using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProductsApplicationLayer.ViewModals;

namespace ProductsApplicationLayer;

public static class DependencyInjection
{
    public static IServiceCollection AddOllamaDocumentAnalyzer(this IServiceCollection services)
    {
        services.AddScoped<IDocumentService, DocumentService>();
        
        // ProductService registration
        services.AddScoped<IProductService, ProductService>(sp =>
        {
            var fileSettings = sp.GetRequiredService<IOptions<FileSettings>>().Value;
            var logger = sp.GetRequiredService<ILogger<ProductService>>();
            return new ProductService(fileSettings.DataFilePath, logger);
        });

        services.AddScoped<ICorpusIndexer, CorpusIndexer>();
        services.AddScoped<IReceiptProcessor, ReceiptProcessor>();
        
        
        return services;
    }
}

