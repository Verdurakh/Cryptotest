using CryptoTest.Models;
using CryptoTest.Models.OrderBooks;
using CryptoTest.Models.Transaction;

namespace CryptoTest.Services;

public class CryptoBuyingStrategy
{
    public static Transaction CreateTransactionStrategy(Exchange exchange, Order order)
    {
        if (order.Type == OrderTypeEnum.Buy.ToString())
            return CreateBuyingStrategy(exchange.OrderBook.Asks.Where(ask => ask.Order.Price <= order.Price), order);
        if (order.Type == OrderTypeEnum.Sell.ToString())
            return CreateBuyingStrategy(exchange.OrderBook.Bids.Where(bid => bid.Order.Price >= order.Price), order);

        throw new Exception($"Unsupported order type: {order.Type}");
    }

    private static Transaction CreateBuyingStrategy(IEnumerable<OrderHolder> availableSellingOrders, Order order)
    {
        var askingAmount = order.Amount;

        var transaction = new Transaction
        {
            UnfulfilledAmount = askingAmount,
            FullfillmentId = order.Id,
        };

        foreach (var sellOrder in availableSellingOrders)
        {
            var amountThatCanBeFilledByOrder = Math.Min(transaction.UnfulfilledAmount, sellOrder.Order.Amount);
            var priceToPay = amountThatCanBeFilledByOrder * sellOrder.Order.Price;
            transaction.FullfillmentAmount += amountThatCanBeFilledByOrder;
            transaction.FullfillmentPrice += priceToPay;
            transaction.UnfulfilledAmount -= amountThatCanBeFilledByOrder;

            transaction.TransactionOrders.Add(new TransactionOrder()
            {
                TransactionAmount = amountThatCanBeFilledByOrder,
                TransactionPrice = priceToPay,
                OrderId = sellOrder.Order.Id,
                OrderRemainingAmount = sellOrder.Order.Amount - amountThatCanBeFilledByOrder,
                OrderOriginalAmount = sellOrder.Order.Amount,
                OrderPrice = sellOrder.Order.Price
            });

            if (transaction.UnfulfilledAmount == 0)
                break;
        }

        return transaction;
    }
}