namespace CryptoTest.Models.OrderBooks;

public record Exchange
{
    public string Id { get; init; }
    public AvailableFunds AvailableFunds { get; init; }
    public OrderBook OrderBook { get; init; }
}