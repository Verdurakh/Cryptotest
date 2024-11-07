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


    private Transaction CreateStrategyMultiExchange(List<(Exchange exchange, OrderHolder orders)> waitingOrders, Order order)
    {
        var transaction = InitializeTransaction(order);

        foreach (var waitingOrder in waitingOrders)
        {
            if (!TryToUseOrderToFillTransaction(transaction, waitingOrder))
                continue;

            if (transaction.UnfulfilledAmount != 0)
                continue;

            logger.LogInformation("We got everything we wanted");
            break;
        }

        return transaction;
    }

    private bool TryToUseOrderToFillTransaction(Transaction transaction, (Exchange exchange, OrderHolder orders) sellOrder)
    {
        var amountThatCanBeFilledByOrder = CalculateAmountThatWeCanUse(transaction, sellOrder);
        if (amountThatCanBeFilledByOrder == 0)
        {
            logger.LogInformation("Exchange: {ExchangeId} : No more bitcoin to use", sellOrder.exchange.Id);
            return false;
        }

        var priceToPay = amountThatCanBeFilledByOrder * sellOrder.orders.Order.Price;

        var (isPriceAdjusted, newPriceToUse) =
            AreWeUsingMoreFundsThenExistsOnExchange(transaction, sellOrder, priceToPay);
        if (isPriceAdjusted)
        {
            amountThatCanBeFilledByOrder = newPriceToUse / sellOrder.orders.Order.Price;
            priceToPay = newPriceToUse;
        }

        if (priceToPay == 0)
        {
            logger.LogInformation("Exchange: {ExchangeId} : No more money to use", sellOrder.exchange.Id);
            return false;
        }

        SetTransactionValues(transaction, sellOrder, amountThatCanBeFilledByOrder, priceToPay);

        logger.LogInformation(
            "Transaction added: {AmountThatCanBeFilledByOrder} btc for {PriceToPay} eur, Exchange: {ExchangeId}",
            amountThatCanBeFilledByOrder, priceToPay, sellOrder.exchange.Id);

        return true;
    }

    private static void SetTransactionValues(Transaction transaction, (Exchange exchange, OrderHolder orders) sellOrder,
        decimal amountThatCanBeFilledByOrder, decimal priceToPay)
    {
        transaction.FullfillmentAmount += amountThatCanBeFilledByOrder;
        transaction.FullfillmentPrice += priceToPay;
        transaction.UnfulfilledAmount -= amountThatCanBeFilledByOrder;

        transaction.TransactionOrders.Add(
            CreateTransactionItem(amountThatCanBeFilledByOrder, priceToPay, sellOrder));

        SetExchangeAmountUsage(transaction, sellOrder, amountThatCanBeFilledByOrder);
        SetExchangePriceUsage(transaction, sellOrder, priceToPay);
    }

    private static Transaction InitializeTransaction(Order order)
    {
        var transaction = new Transaction
        {
            UnfulfilledAmount = order.Amount,
            FullfillmentId = order.Id,
            Type = order.Type
        };
        return transaction;
    }

    private static decimal CalculateAmountThatWeCanUse(Transaction transaction,
        (Exchange exchange, OrderHolder orders) sellOrder)
    {
        var amountThatCanBeFilledByOrder = GetAmountWeCanTakeFromThisOrder(transaction, sellOrder);
        var result = AreWeUsingMoreAmountThenExistsOnExchange(transaction, sellOrder, amountThatCanBeFilledByOrder);
        if (result.isAmountAdjusted)
        {
            amountThatCanBeFilledByOrder = result.newAmount;
        }

        return amountThatCanBeFilledByOrder;
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

    private static (bool isPriceAdjusted, decimal priceToUse) AreWeUsingMoreFundsThenExistsOnExchange(
        Transaction transaction,
        (Exchange exchange, OrderHolder orders) sellOrder, decimal priceToPay)
    {
        if (priceToPay > sellOrder.exchange.AvailableFunds.Euro)
        {
            return (true, sellOrder.exchange.AvailableFunds.Euro);
        }

        if (!transaction.ExchangePriceUsage.TryGetValue(sellOrder.exchange.Id, out var exchangePriceUsed))
            return (false, priceToPay);

        if (exchangePriceUsed + priceToPay >= sellOrder.exchange.AvailableFunds.Euro)
        {
            return (true, sellOrder.exchange.AvailableFunds.Euro - exchangePriceUsed);
        }

        return (false, priceToPay);
    }


    private static (bool isAmountAdjusted, decimal newAmount) AreWeUsingMoreAmountThenExistsOnExchange(
        Transaction transaction,
        (Exchange exchange, OrderHolder orders) sellOrder, decimal amountThatCanBeFilledByOrder
    )
    {
        if (amountThatCanBeFilledByOrder > sellOrder.exchange.AvailableFunds.Crypto)
        {
            return (true, sellOrder.exchange.AvailableFunds.Crypto);
        }

        if (!transaction.ExchangeAmountUsage.TryGetValue(sellOrder.exchange.Id, out var exchangeAmountUsed))
            return (false, amountThatCanBeFilledByOrder);

        if (exchangeAmountUsed + amountThatCanBeFilledByOrder >= sellOrder.exchange.AvailableFunds.Crypto)
        {
            return (true, sellOrder.exchange.AvailableFunds.Crypto - exchangeAmountUsed);
        }

        return (false, amountThatCanBeFilledByOrder);
    }

    private static decimal GetAmountWeCanTakeFromThisOrder(Transaction transaction,
        (Exchange exchange, OrderHolder orders) sellOrder)
    {
        return Math.Min(transaction.UnfulfilledAmount, sellOrder.orders.Order.Amount);
    }
}