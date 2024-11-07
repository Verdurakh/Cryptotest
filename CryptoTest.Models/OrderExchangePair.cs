using CryptoTest.Models.OrderBooks;

namespace CryptoTest.Models;

public struct OrderExchangePair
{
    public Exchange Exchange { get; }
    public OrderHolder OrderHolder { get; }

    public OrderExchangePair(Exchange exchange, OrderHolder orderHolder)
    {
        Exchange = exchange;
        OrderHolder = orderHolder;
    }
}