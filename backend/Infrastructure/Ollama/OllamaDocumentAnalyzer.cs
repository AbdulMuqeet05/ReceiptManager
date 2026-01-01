using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ProductsApplicationLayer;
using ProductsApplicationLayer.ViewModals;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Ollama;

public class OllamaDocumentAnalyzer : IDocumentAnalyzer
    
{
    
    private readonly ILogger<OllamaDocumentAnalyzer> logger;
    private readonly HttpClient _httpClient;
    private const string ModelName = "BGE-M3"; // The model we pulled in Ollama
    private readonly string _groqApiKey;
    public OllamaDocumentAnalyzer(HttpClient httpClient, ILogger<OllamaDocumentAnalyzer> logger, IConfiguration configuration)
    {
        this._httpClient = httpClient;
        this.logger = logger;
        _groqApiKey = configuration["GroqApiKey"] ?? "default_if_missing";
    }
    private const string generic_image_prompt = @"### ROLE
                                                You are a World-Class Retail Data Extraction Engine. Your goal is to convert messy, unstructured OCR text from retail receipts into perfect, machine-readable JSON.

                                                ### EXTRACTION ARCHITECTURE
                                                1. **Full-Line Context (Horizontal Scan):** For every price found, look at the entire horizontal axis to its left. You must capture ALL descriptors (e.g., ""3,8%"", ""BIO"", ""1.5kg"", ""JA!"", ""Fat content""). A product name is incomplete without its qualifiers.
                                                   
                                                2. **The ""Orphan"" Rule (Vertical Merging):**
                                                   If a line contains product specs (measurements like ""1kg"", origins like ""DEUTSCHLAND"", or brand info) but NO price, it is an ""Orphan Fragment."" Append this fragment to the product name on the line directly above or below that has a price.
                                                   
                                                3. **Multiplier Logic (Binding):**
                                                   Lines with patterns like ""2 x 1,99"" or ""2 Stk x 0,99"" MUST be bound to the product name that follows them. 
                                                   - Extract `stk` (Quantity), `unit_price`, and `total_price`.
                                                   - If no multiplier is found, `stk` defaults to 1.

                                                4. **Noise Filtering:**
                                                   Strip out tax category letters (A, B, C), internal SKU numbers (e.g., 40123...), and separator symbols (***, ===).

                                                ### CONSTRAINTS
                                                - Return **ONLY** a valid JSON object.
                                                - No conversational filler or introductions.
                                                - If a value is missing or unclear, use `null` (except for `stk`, which defaults to 1).
                                                - Maintain separate entries for duplicate items found on different lines.

                                                ### JSON SCHEMA
                                                {
                                                  ""merchant"": ""string"",
                                                  ""items"": [
                                                    {
                                                      ""name"": ""string (Full name with all attributes)"",
                                                      ""stk"": number,
                                                      ""unit_price"": number,
                                                      ""total_price"": number
                                                    }
                                                  ],
                                                  ""grand_total"": number,
                                                  ""currency"": ""EUR""
                                                }

                                                ### EXAMPLE GROUND TRUTH
                                                Input:
                                                ""BIO H-MILCH 3,8% ....... 1,25""
                                                ""2 x 1,99""
                                                ""SCHOKOTROEPFCHEN ....... 3,98""
                                                ""SUMME EUR 5,23""

                                                Output:
                                                {
                                                  ""merchant"": ""Unknown"",
                                                  ""items"": [
                                                    {""name"": ""BIO H-MILCH 3,8%"", ""stk"": 1, ""unit_price"": 1.25, ""total_price"": 1.25},
                                                    {""name"": ""SCHOKOTROEPFCHEN"", ""stk"": 2, ""unit_price"": 1.99, ""total_price"": 3.98}
                                                  ],
                                                  ""grand_total"": 5.23,
                                                  ""currency"": ""EUR""
                                                }";
    public async Task<OllamaReceiptRoot> AnalyzeAsync(List<string> base64Images)
    {
        // Try 1: The Standard Extraction
        var firstResult = await CallOllamaGenerate(generic_image_prompt, base64Images);
        var validation = VerifyMath(firstResult);

        if (validation == "true") return firstResult;

        // Try 2: The Correction (No Context, Fresh Request)
        logger.LogWarning("Math error detected: {Error}. Retrying with fresh memory...", validation);
    
        string correctionPrompt = $"{generic_image_prompt}\n\nIMPORTANT CORRECTION: " +
                                  $"In your last attempt, you made a math error: {validation}. " +
                                  $"Please fix the item quantity and prices in the JSON.";

        return await CallOllamaGenerate(correctionPrompt, base64Images);
    }

    public async Task<float[]> GetEmbeddingAsync(string text)
    {
        var request = new
        {
            Model = ModelName,
            prompt = text,
        };
        
        // Call Ollama's embedding endpoint
        var response = await _httpClient.PostAsJsonAsync("api/embeddings", request);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Ollama error: {error}");
        }
        var result = await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>();

        return result?.Embedding ?? throw new Exception("Failed to retrieve embedding from Ollama.");
    }

    private async Task<OllamaReceiptRoot> CallOllamaGenerate(string user_prompt, List<string> images)
    {
        var payload = new {
            model = "qwen2.5vl:7b",
            prompt = user_prompt,
            stream = false,
            format = "json",
            images = images.ToArray(),
            options = new { 
                num_ctx = 8192, 
                temperature = 0.0, // Critical for consistent OCR
                num_gpu = 99,
                main_gpu = 0,
                low_vram = false,
                num_predict = 1000
            }
        };

        var response = await _httpClient.PostAsJsonAsync("api/generate", payload);
    
        // If you get a 500 here, your M4 is truly out of memory. 
        // Try closing other apps (like Chrome) or reducing image resolution.
        if (!response.IsSuccessStatusCode) throw new Exception($"Ollama 500 Error: {await response.Content.ReadAsStringAsync()}");

        var ollamaResult = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>();
        return DeserializeResponse(ollamaResult.Response);
    }
    
    
    public async Task<OllamaReceiptRoot> ProcessReceiptWithGroq(List<string> base64Image)
    {
        using var client = new HttpClient();
        client.BaseAddress = new Uri("https://api.groq.com/openai/v1/");
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_groqApiKey}");

        // 1. Create the content array starting with the prompt
        var contentArray = new List<object>
        {
            new { type = "text", text = generic_image_prompt }
        };

        // 2. Add EACH image from your list to that same array
        foreach (var b64 in base64Image)
        {
            // Trim and remove any hidden newlines that C# might have added
            string cleanBase64 = b64.Trim().Replace("\r", "").Replace("\n", "");
        
            contentArray.Add(new { 
                type = "image_url", 
                image_url = new { url = $"data:image/jpeg;base64,{cleanBase64}" } 
            });
        }

        // 3. The final payload uses 'contentArray' directly
        var payload = new {
            model = "meta-llama/llama-4-scout-17b-16e-instruct",
            messages = new[] {
                new { 
                    role = "user", 
                    content = contentArray // Pass the whole list here
                }
            },
            temperature = 0.1,
            response_format = new { type = "json_object" },
            max_completion_tokens = 4096
        };

        var response = await client.PostAsJsonAsync("chat/completions", payload);
        if (!response.IsSuccessStatusCode)
        {
            // THIS LINE WILL TELL YOU THE REAL PROBLEM:
            string errorBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Groq Error: {errorBody}");   
        }
        // 1. Deserialize using the correct Groq/OpenAI class
        var result = await response.Content.ReadFromJsonAsync<GroqChatResponse>();
        // 2. Extract the text from the choices array
        string aiText = result?.GetText() ?? "";

        logger.LogInformation("Groq receive complete: " + aiText);

        return DeserializeResponse(aiText);
    }
    private string VerifyMath(OllamaReceiptRoot root)
    {
        if (root?.Items == null || !root.Items.Any()) return "No items found";
        string retrunValue = "";
        double calculatedGrandTotal = 0;
        foreach (var item in root.Items)
        {
            double lineTotal = Math.Round((double)item.Quantity * (double)item.UnitPrice, 2);
            if (Math.Abs(lineTotal - (double)item.TotalPrice) > 0.01)
            {
                retrunValue += $"Math error at '{item.Name}': {item.Quantity} x {item.UnitPrice} is {lineTotal} but in the Receipt it is {item.TotalPrice} Either the stk or unit_price or total Price please re check this.  ";
            }
            calculatedGrandTotal += lineTotal;
        }

        if (Math.Abs(calculatedGrandTotal - (double)root.GrandTotal) > 0.05)
        {
            retrunValue += $"Grand total mismatch: Sum of items is {calculatedGrandTotal} but grand_total is {root.GrandTotal}";
        }
        else
        {
            retrunValue = "true";
        }

        return retrunValue;
    }

    private OllamaReceiptRoot DeserializeResponse(string rawResponse)
    {
        try {
            string cleanJson = rawResponse.Trim();
            if (cleanJson.StartsWith("```json"))
                cleanJson = cleanJson.Replace("```json", "").Replace("```", "").Trim();

            return JsonSerializer.Deserialize<OllamaReceiptRoot>(cleanJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true, AllowTrailingCommas = true });
        } catch {
            return new OllamaReceiptRoot { Items = new List<DocumentItems>() };
        }
    }
}