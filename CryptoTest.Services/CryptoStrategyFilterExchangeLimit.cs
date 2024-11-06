using CryptoTest.Models.OrderBooks;

namespace CryptoTest.Services;

/// <summary>
/// We cannot have orders that exceed the available funds on the exchange.
/// </summary>
public class CryptoStrategyFilterExchangeLimit
{
    public static Exchange FilterExchangeLimit(Exchange exchange)
    {
        var restrictedSellOrders = exchange.OrderBook.Asks
            .Where(ask => ask.Order.Price * ask.Order.Amount <= exchange.AvailableFunds.Euro).ToList();
        var restrictedBuyOrders =
            exchange.OrderBook.Bids.Where(bid => bid.Order.Amount <= exchange.AvailableFunds.Crypto).ToList();

        exchange.OrderBook.Asks = restrictedSellOrders;
        exchange.OrderBook.Bids = restrictedBuyOrders;
        return exchange;
    }
}