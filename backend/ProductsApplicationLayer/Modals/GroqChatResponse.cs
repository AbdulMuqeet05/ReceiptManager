using System.Text.Json.Serialization;

namespace ProductsApplicationLayer.ViewModals;

public class GroqChatResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("choices")]
    public List<GroqChoice> Choices { get; set; } = new();

    // Helper property to get the text easily
    public string GetText() => Choices.FirstOrDefault()?.Message?.Content ?? "";
}

public class GroqChoice
{
    [JsonPropertyName("message")]
    public GroqMessage Message { get; set; } = new();
}

public class GroqMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}