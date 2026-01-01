using Microsoft.Extensions.Logging;
using ProductsApplicationLayer.ViewModals;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace ProductsApplicationLayer;
using Microsoft.VisualBasic.FileIO;

public class ProductService : IProductService
{
    private readonly string _filePath;
    private readonly ILogger<ProductService> _logger;

    public ProductService(string filePath, ILogger<ProductService> logger)
    {
        _filePath = filePath;
        _logger = logger;
    }
    public IEnumerable<ProductViewModal> GetAllProducts()
    {
        if (!File.Exists(_filePath))
        {
            _logger.LogError("Product file not found at path: " + _filePath);
            return Enumerable.Empty<ProductViewModal>();
        }

        return ReadCsvInternal();
    }

    private IEnumerable<ProductViewModal> ReadCsvInternal()
    {
        // Use a try-catch here to provide better logging for debugging
        try 
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                // If the CSV has extra spaces around commas, this cleans them
                TrimOptions = TrimOptions.Trim, 
                HeaderValidated = null,
                MissingFieldFound = null
            };

            using var reader = new StreamReader(_filePath);
            using var csv = new CsvReader(reader, config);
        
            csv.Context.RegisterClassMap<ProductMap>();

            // ToList() is essential! It forces the library to parse the file 
            // while the StreamReader is still open.
            var products = csv.GetRecords<ProductViewModal>().ToList();
            Console.WriteLine("Number of record retrieved from the CSV : " + products.Count);
            return products;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Critical error reading CSV at {Path}", _filePath);
            throw; // Re-throw so your API returns a 500 error instead of a blank page
        }
    }
    
}