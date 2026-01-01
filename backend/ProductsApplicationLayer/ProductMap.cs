using System.Globalization;
using ProductsApplicationLayer.ViewModals;

namespace ProductsApplicationLayer;
using CsvHelper.Configuration;

public sealed class ProductMap : ClassMap<ProductViewModal>
{
    public ProductMap()
    {
        Map(m => m.Category).Name("Category");
        Map(m => m.Title).Name("Title");
        
        // Handle "1,49" -> 1.49f
        Map(m => m.Price).Name("Price_Euro").Convert(args => 
        {
            var raw = args.Row.GetField("Price_Euro");
            if (string.IsNullOrWhiteSpace(raw)) return (decimal)0f;
            
            // Clean the string: remove quotes and swap comma for dot
            var clean = raw.Replace("\"", "").Replace(",", ".").Trim();
            
            return (decimal)(float.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) 
                ? result 
                : 0f);
        });

        Map(m => m.Grammage).Name("Grammage");
        Map(m => m.VectorId).Name("Product_ID");
    }
}