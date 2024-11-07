using System.ComponentModel.DataAnnotations;
using CryptoTest.Models.Enums;

namespace CryptoTest.Models.OrderBooks;

public class Order : IValidatableObject
{
    public Guid Id { get; set; }
    public DateTime Time { get; set; }
    [Required] public string Type { get; set; }
    public string Kind { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be a positive value.")]
    public decimal Amount { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be a positive value.")]
    public decimal Price { get; set; }

    /// <summary>
    /// Yes yes I know
    /// </summary>
    /// <param name="validationContext"></param>
    /// <returns></returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Check if Type is either "1" or "2"
        if (Type != OrderTypeEnum.Buy.ToString() && Type != OrderTypeEnum.Sell.ToString())
        {
            yield return new ValidationResult(
                $"Type must be either '{OrderTypeEnum.Buy.ToString()}' or '{OrderTypeEnum.Sell.ToString()}'.",
                new[] {nameof(Type)});
        }
    }
}