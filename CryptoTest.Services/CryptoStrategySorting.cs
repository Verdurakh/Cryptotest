using CryptoTest.Models.OrderBooks;

namespace CryptoTest.Services;

public class CryptoStrategySorting
{
    public static Exchange SortExchangeOrders(Exchange exchange)
    {
        var sortedBidsHigh = exchange.OrderBook.Bids.OrderByDescending(bid => bid.Order.Price).ToList();
        var sortedAsksLow = exchange.OrderBook.Asks.OrderBy(ask => ask.Order.Price).ToList();

        exchange.OrderBook.Asks = sortedAsksLow;
        exchange.OrderBook.Bids = sortedBidsHigh;
        return exchange;
    }
}