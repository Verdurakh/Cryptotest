namespace CryptoTest.Models.OrderBooks;

public record AvailableFunds
{
    public decimal Crypto { get; init; }
    public decimal Euro { get; init; }
}