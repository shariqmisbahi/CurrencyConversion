namespace CurrencyConversion.Models
{
    /// <summary>
    /// Represents the result of a currency conversion
    /// </summary>
    public class ConversionResult
    {
        /// <summary>
        /// The original currency code
        /// </summary>
        public required string FromCurrency { get; set; }

        /// <summary>
        /// The target currency code
        /// </summary>
        public required string ToCurrency { get; set; }

        /// <summary>
        /// The original amount to convert
        /// </summary>
        public decimal OriginalAmount { get; set; }

        /// <summary>
        /// The converted amount
        /// </summary>
        public decimal ConvertedAmount { get; set; }

        /// <summary>
        /// The conversion rate used
        /// </summary>
        public decimal ConversionRate { get; set; }
    }
}