using System.Globalization;

namespace HabitatScraper.Utils.Helpers
{
    public class ParsePriceHelper
    {
        public static decimal ParsePrice(string priceText)
        {
            //input example "1.100 € /mes"
            var cleanedText = priceText.Replace("€", "")
                                       .Replace("/mes", "")
                                       .Replace(".", "")
                                       .Trim();
            return decimal.TryParse(cleanedText, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var price)
                ? price
                : 0;
        }
    }
}
