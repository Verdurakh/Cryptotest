using CryptoTest.Models;
using CryptoTest.Models.Enums;
using CryptoTest.Models.OrderBooks;
using CryptoTest.Models.Transaction;
using Microsoft.Extensions.Logging;

namespace CryptoTest.Services.StrategyService;

public class CryptoTransactionStrategy(ILogger<CryptoTransactionStrategy> logger) : ICryptoTransactionStrategy
{
    public Transaction CreateTransactionStrategy(Exchange exchange, Order order)
    {
        return CreateTransactionStrategy(new List<Exchange> {exchange}, order);
    }

    /// <summary>
    /// Creates a transaction strategy for a given order in the given exchanges
    /// </summary>
    /// <param name="exchanges"></param>
    /// <param name="order"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public Transaction CreateTransactionStrategy(IEnumerable<Exchange> exchanges, Order order)
    {
        if (order.Type == OrderTypeEnum.Buy.ToString())
        {
            var availableSellingOrders = exchanges
                .SelectMany(exchange => exchange.OrderBook.Asks.Select(ask => (Exchange: exchange, OrderHolder: ask)))
                .Where(orderExchange => orderExchange.OrderHolder.Order.Price <= order.Price)
                .OrderBy(orderExchange => orderExchange.OrderHolder.Order.Price)
                .ToList();

            return CreateStrategyMultiExchange(availableSellingOrders, order);
        }

        if (order.Type == OrderTypeEnum.Sell.ToString())
        {
            var availableBuyingOrders = exchanges
                .SelectMany(exchange => exchange.OrderBook.Bids.Select(ask => (Exchange: exchange, OrderHolder: ask)))
                .Where(orderExchange => orderExchange.OrderHolder.Order.Price >= order.Price)
                .OrderByDescending(orderExchange => orderExchange.OrderHolder.Order.Price)
                .ToList();

            return CreateStrategyMultiExchange(availableBuyingOrders, order);
        }


        throw new Exception($"Unsupported order type: {order.Type}");
    }


    private Transaction CreateStrategyMultiExchange(List<(Exchange exchange, OrderHolder orders)> orders, Order order)
    {
        var askingAmount = order.Amount;

        var transaction = new Transaction
        {
            UnfulfilledAmount = askingAmount,
            FullfillmentId = order.Id,
            Type = order.Type
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
                logger.LogInformation("Exchange: {ExchangeId} : No more bitcoin to use", sellOrder.exchange.Id);
                continue;
            }


            var priceToPay = amountThatCanBeFilledByOrder * sellOrder.orders.Order.Price;

            if (AreWeUsingMoreFundsThenExistsOnExchange(transaction, sellOrder, priceToPay, out var exchangePriceUsed))
            {
                priceToPay = sellOrder.exchange.AvailableFunds.Euro - exchangePriceUsed;
                //If we are adjusting how much funds we use we need to adjust the amount of bitcoin we buy
                //This should result in fewer bitcoins then before so we probably don't need to check available coins
                amountThatCanBeFilledByOrder = priceToPay / sellOrder.orders.Order.Price;
            }


            if (priceToPay == 0)
            {
                logger.LogInformation("Exchange: {ExchangeId} : No more money to use", sellOrder.exchange.Id);
                continue;
            }


            transaction.FullfillmentAmount += amountThatCanBeFilledByOrder;
            transaction.FullfillmentPrice += priceToPay;
            transaction.UnfulfilledAmount -= amountThatCanBeFilledByOrder;

            transaction.TransactionOrders.Add(
                CreateTransactionItem(amountThatCanBeFilledByOrder, priceToPay, sellOrder));

            SetExchangeAmountUsage(transaction, sellOrder, amountThatCanBeFilledByOrder);
            SetExchangePriceUsage(transaction, sellOrder, priceToPay);

            logger.LogInformation(
                "Transaction added: {AmountThatCanBeFilledByOrder} btc for {PriceToPay} eur, Exchange: {ExchangeId}",
                amountThatCanBeFilledByOrder, priceToPay, sellOrder.exchange.Id);


            if (transaction.UnfulfilledAmount != 0)
                continue;

            logger.LogInformation("We got everything we wanted");
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
        if (priceToPay > sellOrder.exchange.AvailableFunds.Euro)
        {
            exchangePriceUsed = 0;
            return true;
        }

        return transaction.ExchangePriceUsage.TryGetValue(sellOrder.exchange.Id, out exchangePriceUsed) &&
               exchangePriceUsed + priceToPay >= sellOrder.exchange.AvailableFunds.Euro;
    }

    private static bool AreWeUsingMoreAmountThenExistsOnExchange(Transaction transaction,
        (Exchange exchange, OrderHolder orders) sellOrder, decimal amountThatCanBeFilledByOrder,
        out decimal exchangeAmountUsed)
    {
        if (amountThatCanBeFilledByOrder > sellOrder.exchange.AvailableFunds.Crypto)
        {
            exchangeAmountUsed = 0;
            return true;
        }

        return transaction.ExchangeAmountUsage.TryGetValue(sellOrder.exchange.Id, out exchangeAmountUsed) &&
               exchangeAmountUsed + amountThatCanBeFilledByOrder >= sellOrder.exchange.AvailableFunds.Crypto;
    }

    private static decimal GetAmountWeCanTakeFromThisOrder(Transaction transaction,
        (Exchange exchange, OrderHolder orders) sellOrder)
    {
        return Math.Min(transaction.UnfulfilledAmount, sellOrder.orders.Order.Amount);
    }
}