using System.ComponentModel.DataAnnotations;

namespace CurrencyConversion.Models
{
    /// <summary>
    /// Represents a currency conversion request
    /// </summary>
    public class ConversionRequest
    {
        /// <summary>
        /// The currency code to convert from (e.g., "USD")
        /// </summary>
        [Required(ErrorMessage = "FromCurrency is required")]
        [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency code must be 3 characters")]
        [RegularExpression(@"^[A-Z]{3}$", ErrorMessage = "Currency code must be 3 uppercase letters")]
        public required string FromCurrency { get; set; }

        /// <summary>
        /// The currency code to convert to (e.g., "EUR")
        /// </summary>
        [Required(ErrorMessage = "ToCurrency is required")]
        [StringLength(3, MinimumLength = 3, ErrorMessage = "Currency code must be 3 characters")]
        [RegularExpression(@"^[A-Z]{3}$", ErrorMessage = "Currency code must be 3 uppercase letters")]
        public required string ToCurrency { get; set; }

        /// <summary>
        /// The amount to convert
        /// </summary>
        [Required(ErrorMessage = "Amount is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }
    }
}