namespace CryptoTest.Models.OrderBooks;


public class OrderBook
{
    public List<OrderHolder> Bids { get; set; }
    public List<OrderHolder> Asks { get; set; }
}