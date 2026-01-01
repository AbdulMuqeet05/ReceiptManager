using System.Globalization;

namespace ProductsApplicationLayer.Helper;

public class Helper
{
    public decimal ParseGermanPrice(string rawPrice)
    {
        // 1. Remove letters (like 'B') and spaces
        // 2. Replace comma with dot
        string clean = new string(rawPrice.Where(c => char.IsDigit(c) || c == ',' || c == '.').ToArray());
        clean = clean.Replace(",", ".");
    
        return decimal.Parse(clean, CultureInfo.InvariantCulture);
    }
}