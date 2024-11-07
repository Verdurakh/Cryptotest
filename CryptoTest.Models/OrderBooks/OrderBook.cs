namespace CryptoTest.Models.OrderBooks;


public record OrderBook
{
    public List<OrderHolder> Bids { get; init; }
    public List<OrderHolder> Asks { get; init; }
}