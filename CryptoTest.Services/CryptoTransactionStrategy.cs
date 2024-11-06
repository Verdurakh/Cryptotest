using CryptoTest.Models;
using CryptoTest.Models.OrderBooks;
using CryptoTest.Models.Transaction;

namespace CryptoTest.Services;

public class CryptoTransactionStrategy : ICryptoTransactionStrategy
{
    public Transaction CreateTransactionStrategy(Exchange exchange, Order order)
    {
        if (order.Type == OrderTypeEnum.Buy.ToString())
            return CreateStrategy(exchange.OrderBook.Asks.Where(ask => ask.Order.Price <= order.Price), order,
                exchange.Id);
        if (order.Type == OrderTypeEnum.Sell.ToString())
            return CreateStrategy(exchange.OrderBook.Bids.Where(bid => bid.Order.Price >= order.Price), order,
                exchange.Id);

        throw new Exception($"Unsupported order type: {order.Type}");
    }

    public Transaction CreateTransactionStrategy(IEnumerable<Exchange> exchanges, Order order)
    {
        if (order.Type == OrderTypeEnum.Buy.ToString())
        {
            var availableSellingOrders = exchanges
                .SelectMany(exchange => exchange.OrderBook.Asks.Select(ask => (Exchange: exchange, OrderHolder: ask)))
                .Where(x => x.OrderHolder.Order.Price <= order.Price)
                .OrderBy(x => x.OrderHolder.Order.Price)
                .ToList();

            return CreateStrategyMulti(availableSellingOrders, order);
        }

        if (order.Type == OrderTypeEnum.Sell.ToString())
        {
            var availableSellingOrders = exchanges
                .SelectMany(exchange => exchange.OrderBook.Bids.Select(ask => (Exchange: exchange, OrderHolder: ask)))
                .Where(x => x.OrderHolder.Order.Price >= order.Price)
                .OrderByDescending(x => x.OrderHolder.Order.Price)
                .ToList();

            return CreateStrategyMulti(availableSellingOrders, order);
        }


        throw new Exception($"Unsupported order type: {order.Type}");
    }


    private Transaction CreateStrategyMulti(List<(Exchange exchange, OrderHolder orders)> orders, Order order)
    {
        var askingAmount = order.Amount;

        var transaction = new Transaction
        {
            UnfulfilledAmount = askingAmount,
            FullfillmentId = order.Id,
        };
        foreach (var sellOrder in orders)
        {
            var amountThatCanBeFilledByOrder = GetAmountWeCanTakeFromThisOrder(transaction, sellOrder);

            if (AreWeUsingMoreAmountThenExistsOnExchange(transaction, sellOrder, amountThatCanBeFilledByOrder,
                    out var exchangeAmountUsed))
            {
                amountThatCanBeFilledByOrder = sellOrder.exchange.AvailableFunds.Crypto - exchangeAmountUsed;
            }


            if (amountThatCanBeFilledByOrder == 0)
            {
                Console.WriteLine($"Exchange: {sellOrder.exchange.Id} : No more bitcoin to use");
                continue;
            }


            var priceToPay = amountThatCanBeFilledByOrder * sellOrder.orders.Order.Price;

            if (AreWeUsingMoreFundsThenExistsOnExchange(transaction, sellOrder, priceToPay, out var exchangePriceUsed))
            {
                priceToPay = sellOrder.exchange.AvailableFunds.Euro - exchangePriceUsed;
                //If we are adjusting how much funds we use we need to adjust the amount of bitcoin we buy
                amountThatCanBeFilledByOrder = priceToPay / sellOrder.orders.Order.Price;
            }


            if (priceToPay == 0)
            {
                Console.WriteLine($"Exchange: {sellOrder.exchange.Id} : No more money to use");
                continue;
            }


            transaction.FullfillmentAmount += amountThatCanBeFilledByOrder;
            transaction.FullfillmentPrice += priceToPay;
            transaction.UnfulfilledAmount -= amountThatCanBeFilledByOrder;

            transaction.TransactionOrders.Add(
                CreateTransactionItem(amountThatCanBeFilledByOrder, priceToPay, sellOrder));

            SetExchangeAmountUsage(transaction, sellOrder, amountThatCanBeFilledByOrder);
            SetExchangePriceUsage(transaction, sellOrder, priceToPay);

            Console.WriteLine(
                $"Transaction added: {amountThatCanBeFilledByOrder} btc for {priceToPay} eur, Exchange: {sellOrder.exchange.Id}");


            if (transaction.UnfulfilledAmount != 0)
                continue;

            Console.WriteLine("We got everyhing we wanted");
            break;
        }

        return transaction;
    }

    private static void SetExchangePriceUsage(Transaction transaction,
        (Exchange exchange, OrderHolder orders) sellOrder,
        decimal priceToPay)
    {
        transaction.ExchangePriceUsage.TryAdd(sellOrder.exchange.Id, 0);
        transaction.ExchangePriceUsage[sellOrder.exchange.Id] += priceToPay;
    }

    private static void SetExchangeAmountUsage(Transaction transaction,
        (Exchange exchange, OrderHolder orders) sellOrder,
        decimal amountThatCanBeFilledByOrder)
    {
        transaction.ExchangeAmountUsage.TryAdd(sellOrder.exchange.Id, 0);
        transaction.ExchangeAmountUsage[sellOrder.exchange.Id] += amountThatCanBeFilledByOrder;
    }

    private static TransactionOrder CreateTransactionItem(decimal amountThatCanBeFilledByOrder, decimal priceToPay,
        (Exchange exchange, OrderHolder orders) sellOrder)
    {
        return new TransactionOrder()
        {
            TransactionAmount = amountThatCanBeFilledByOrder,
            TransactionPrice = priceToPay,
            OrderId = sellOrder.orders.Order.Id,
            OrderRemainingAmount = sellOrder.orders.Order.Amount - amountThatCanBeFilledByOrder,
            OrderOriginalAmount = sellOrder.orders.Order.Amount,
            OrderPrice = sellOrder.orders.Order.Price,
            Exchange = sellOrder.exchange.Id
        };
    }

    private static bool AreWeUsingMoreFundsThenExistsOnExchange(Transaction transaction,
        (Exchange exchange, OrderHolder orders) sellOrder, decimal priceToPay, out decimal exchangePriceUsed)
    {
        return transaction.ExchangePriceUsage.TryGetValue(sellOrder.exchange.Id, out exchangePriceUsed) &&
               exchangePriceUsed + priceToPay >= sellOrder.exchange.AvailableFunds.Euro;
    }

    private static bool AreWeUsingMoreAmountThenExistsOnExchange(Transaction transaction,
        (Exchange exchange, OrderHolder orders) sellOrder, decimal amountThatCanBeFilledByOrder,
        out decimal exchangeAmountUsed)
    {
        return transaction.ExchangeAmountUsage.TryGetValue(sellOrder.exchange.Id, out exchangeAmountUsed) &&
               exchangeAmountUsed + amountThatCanBeFilledByOrder >= sellOrder.exchange.AvailableFunds.Crypto;
    }

    private static decimal GetAmountWeCanTakeFromThisOrder(Transaction transaction,
        (Exchange exchange, OrderHolder orders) sellOrder)
    {
        return Math.Min(transaction.UnfulfilledAmount, sellOrder.orders.Order.Amount);
    }

    private Transaction CreateStrategy(IEnumerable<OrderHolder> availableSellingOrders, Order order, string exchange)
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
                OrderPrice = sellOrder.Order.Price,
                Exchange = exchange
            });

            if (transaction.UnfulfilledAmount == 0)
                break;
        }

        return transaction;
    }
}