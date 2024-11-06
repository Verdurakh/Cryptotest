namespace CryptoTest.Models.OrderBooks;

public class Exchange
{
    public string Id { get; set; }
    public AvailableFunds AvailableFunds { get; set; }
    public OrderBook OrderBook { get; set; }
}