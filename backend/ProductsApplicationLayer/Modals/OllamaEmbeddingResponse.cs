namespace ProductsApplicationLayer.ViewModals;

public class OllamaEmbeddingResponse
{
    // Note: Ollama returns the field as "embedding" (lowercase)
    // .NET JSON serializer handles case-insensitivity by default
    public float[] Embedding { get; set; } = Array.Empty<float>();
}