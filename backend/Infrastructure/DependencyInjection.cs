using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using ProductsApplicationLayer; // Reference your interfaces here
using Infrastructure.Qdrant;
using Infrastructure.Ollama;
using ProductsApplicationLayer;
using Qdrant.Client;

namespace Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
       
        // 1 register the Qdrant Client
        var qdrantUrl = configuration["Qdrant_Url"] ?? "http://qdrant_db:6334";
        var groqApiKey = configuration["GroqApiKey"] ?? "groqapikey";
        
        // 2 Register Ollama HTTP Client 
        services.AddHttpClient<IDocumentAnalyzer, OllamaDocumentAnalyzer>(client =>
        {
            var ollamaUrl = configuration["Ollama_Url"] ??  "http://localhost:11434/";
            var groqApiKey = configuration["GroqApiKey"] ??  "groqapikey";
            client.BaseAddress = new Uri(ollamaUrl);
            client.Timeout = TimeSpan.FromMinutes(7);
        });
        
        AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        services.AddSingleton(new QdrantClient(new Uri(qdrantUrl)));
        services.AddScoped<IVectorSearchService, QdrantVectorService>();
        return services;
    }
}