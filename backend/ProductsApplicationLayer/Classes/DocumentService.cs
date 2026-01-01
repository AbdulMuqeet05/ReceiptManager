using System.Drawing;
using IronSoftware.Drawing;
using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace ProductsApplicationLayer;

public class DocumentService: IDocumentService
{
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(ILogger<DocumentService> logger)
    {
        _logger = logger;
    }
    public async Task<List<string>> ExtractReceiptData(Stream file, string contentType)
    {
        
        // 1. Always reset position if the stream supports seeking
        if (file.CanSeek) file.Position = 0;
        
        List<string> base64Images = new();
        if (contentType == "application/pdf")
        {
            // Return immediately so we don't hit the image logic below
            return await ConvertPdfToImage(file);
        }
        try
        {
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            byte[] imageBytes = ms.ToArray();
            byte[] resizedBytes = ResizeReceiptForAi(imageBytes);
            return new List<string>{ Convert.ToBase64String(resizedBytes) };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            Console.Write("Error reading the stream : "+ ex.Message);
            throw new Exception("Error reading the stream : " + ex.Message);
        }
    }

    private async Task<List<string>> ConvertPdfToImage(Stream pdfStream)
    {
        var base64Images = new List<string>();
        // Reset to start of stream
        if (pdfStream.CanSeek) pdfStream.Position = 0;
        try
        {

            // 1. Load the PDF from the stream using the constructor
            using var ms = new MemoryStream();
            await pdfStream.CopyToAsync(ms);
            
            // Use the bytes to create the IronPdf document
            using var pdf = new PdfDocument(ms.ToArray());

            // 2. Convert each page to a bitmap object
            // ToBitmap() returns an array of AnyBitmap objects
            AnyBitmap[] bitmaps = pdf.ToBitmap();
            foreach (var bitmap in bitmaps)
            {
                // 3. Export the bitmap to bytes in Jpeg format
                // The first parameter is the format, the second is quality (0-100)
                byte[] imageBytes = bitmap.ExportBytes(AnyBitmap.ImageFormat.Jpeg, 90);

                
                byte[] resizedBytes = ResizeReceiptForAi(imageBytes);
                // 4. Convert to Base64 for your AI model
                base64Images.Add(Convert.ToBase64String(resizedBytes));

                // Good practice: dispose of the bitmap to save RAM
                bitmap.Dispose();
            }

            return base64Images;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            throw new Exception("Error reading the stream : " + ex.Message);
        }
        
    }
    
    public byte[] ResizeReceiptForAi(byte[] imageBytes, int maxDimension = 1024)
    {
        using var inputStream = new MemoryStream(imageBytes);
        using var original = SKBitmap.Decode(inputStream);

        // Calculate new dimensions while maintaining aspect ratio
        int width = original.Width;
        int height = original.Height;

        if (width > maxDimension || height > maxDimension)
        {
            if (width > height)
            {
                height = (int)(height * ((float)maxDimension / width));
                width = maxDimension;
            }
            else
            {
                width = (int)(width * ((float)maxDimension / height));
                height = maxDimension;
            }
        }

        // Perform high-quality resize
        using var resized = original.Resize(new SKImageInfo(width, height), SKFilterQuality.Medium);
        using var image = SKImage.FromBitmap(resized);
        using var outputData = image.Encode(SKEncodedImageFormat.Jpeg, 80); // 80% quality is plenty for AI

        return outputData.ToArray();
    }
    
}