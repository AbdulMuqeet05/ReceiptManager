using Microsoft.AspNetCore.Mvc;
using ProductsApplicationLayer;

namespace WebApplication1.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentController : ControllerBase
{
    private readonly ILogger<DocumentController> _logger;
    private readonly IDocumentService _documentService;
    private readonly IDocumentAnalyzer _analyzer;
    private readonly IReceiptProcessor _receiptService;
    
    public DocumentController(ILogger<DocumentController> logger, IDocumentService documentService, IDocumentAnalyzer analyzer, IReceiptProcessor receiptService)
    {
        _logger = logger;
        _documentService = documentService;
        _analyzer = analyzer;
        _receiptService = receiptService;
    }
    // Post
    [HttpPost]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Please upload a valid file.");

        try
        {
            using (var stream = file.OpenReadStream())
            {
                var result = await _documentService.ExtractReceiptData(stream, file.ContentType);
                // Check if we actually got images back
                if (result == null || result.Count == 0)
                {
                    return BadRequest("Could not extract any images from the document.");
                }

                // result[0] is now safe to use
                var response = await _analyzer.AnalyzeAsync(result);
                
                // 3. Vector Search Matching
                // var finalItems = await _receiptService.ProcessAndMatchReceiptAsync(response);
                var finalItems = await _receiptService.MatchReceiptItems(response);
    
                return Ok(new
                {
                    RawGrandTotal = response.GrandTotal,
                    Currency = response.Currency,
                    Items = finalItems
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            Console.Write("Error reading the stream : " + ex.Message);
            return NotFound(ex.Message);
        } 
        
    }

    [HttpPost("groq")]
    public async Task<IActionResult> UploadFileAndCallGroq(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("Please upload a valid file.");

        try
        {
            using (var stream = file.OpenReadStream())
            {
                var result = await _documentService.ExtractReceiptData(stream, file.ContentType);
                // Check if we actually got images back
                if (result == null || result.Count == 0)
                {
                    return BadRequest("Could not extract any images from the document.");
                }

                // result[0] is now safe to use
                var response = await _analyzer.ProcessReceiptWithGroq(result);
                
                // // 3. Vector Search Matching
                var finalItems = await _receiptService.MatchReceiptItems(response);
    
                return Ok(new
                {
                    RawGrandTotal = response.GrandTotal,
                    Currency = response.Currency,
                    Items = finalItems
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            Console.Write("Error reading the stream : " + ex.Message);
            return NotFound(ex.Message);
        } 
    }
    
  
}