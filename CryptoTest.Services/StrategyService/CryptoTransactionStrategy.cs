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
        var orderType = order.Type == OrderTypeEnum.Buy.ToString() ? OrderTypeEnum.Buy : OrderTypeEnum.Sell;
        var availableOrders = GetAvailableOrders(exchanges, order, orderType);

        return CreateStrategyMultiExchange(availableOrders, order);
    }

    private static List<OrderExchangePair> GetAvailableOrders(IEnumerable<Exchange> exchanges, Order order,
        OrderTypeEnum orderType)
    {
        return orderType == OrderTypeEnum.Buy
            ? exchanges
                .SelectMany(exchange => exchange.OrderBook.Asks.Select(ask => new OrderExchangePair(exchange, ask)))
                .Where(pair => pair.OrderHolder.Order.Price <= order.Price)
                .OrderBy(pair => pair.OrderHolder.Order.Price)
                .ToList()
            : exchanges
                .SelectMany(exchange => exchange.OrderBook.Bids.Select(bid => new OrderExchangePair(exchange, bid)))
                .Where(pair => pair.OrderHolder.Order.Price >= order.Price)
                .OrderByDescending(pair => pair.OrderHolder.Order.Price)
                .ToList();
    }


    private Transaction CreateStrategyMultiExchange(List<OrderExchangePair> waitingOrders, Order order)
    {
        var transaction = InitializeTransaction(order);

        foreach (var waitingOrder in waitingOrders)
        {
            if (!CanWeFillOrder(transaction, waitingOrder))
                continue;

            if (transaction.UnfulfilledAmount != 0)
                continue;

            logger.LogInformation("We got everything we wanted");
            break;
        }

        return transaction;
    }

    private bool CanWeFillOrder(Transaction transaction, OrderExchangePair sellOrder)
    {
        var amountThatCanBeFilledByOrder = CalculateAmountThatWeCanUse(transaction, sellOrder);
        if (amountThatCanBeFilledByOrder == 0)
        {
            logger.LogInformation("Exchange: {ExchangeId} : No more bitcoin to use", sellOrder.Exchange.Id);
            return false;
        }

        var priceToPay = amountThatCanBeFilledByOrder * sellOrder.OrderHolder.Order.Price;

        var (isPriceAdjusted, newPriceToUse) =
            AreFundsOnExchangeSufficient(transaction, sellOrder, priceToPay);
        if (isPriceAdjusted)
        {
            amountThatCanBeFilledByOrder = newPriceToUse / sellOrder.OrderHolder.Order.Price;
            priceToPay = newPriceToUse;
        }

        if (priceToPay == 0)
        {
            logger.LogInformation("Exchange: {ExchangeId} : No more money to use", sellOrder.Exchange.Id);
            return false;
        }

        SetTransactionValues(transaction, sellOrder, amountThatCanBeFilledByOrder, priceToPay);

        logger.LogInformation(
            "Transaction added: {AmountThatCanBeFilledByOrder} btc for {PriceToPay} eur, Exchange: {ExchangeId}",
            amountThatCanBeFilledByOrder, priceToPay, sellOrder.Exchange.Id);

        return true;
    }

    private static void SetTransactionValues(Transaction transaction, OrderExchangePair sellOrder,
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
        OrderExchangePair sellOrder)
    {
        var amountThatCanBeFilledByOrder = GetAmountWeCanTakeFromThisOrder(transaction, sellOrder);
        var result = AreCryptoOnExchangeSufficient(transaction, sellOrder, amountThatCanBeFilledByOrder);
        if (result.isAmountAdjusted)
        {
            amountThatCanBeFilledByOrder = result.newAmount;
        }

        return amountThatCanBeFilledByOrder;
    }

    private static void SetExchangePriceUsage(Transaction transaction,
        OrderExchangePair sellOrder,
        decimal priceToPay)
    {
        transaction.ExchangePriceUsage.TryAdd(sellOrder.Exchange.Id, 0);
        transaction.ExchangePriceUsage[sellOrder.Exchange.Id] += priceToPay;
    }

    private static void SetExchangeAmountUsage(Transaction transaction,
        OrderExchangePair sellOrder,
        decimal amountThatCanBeFilledByOrder)
    {
        transaction.ExchangeAmountUsage.TryAdd(sellOrder.Exchange.Id, 0);
        transaction.ExchangeAmountUsage[sellOrder.Exchange.Id] += amountThatCanBeFilledByOrder;
    }

    private static TransactionOrder CreateTransactionItem(decimal amountThatCanBeFilledByOrder, decimal priceToPay,
        OrderExchangePair sellOrder)
    {
        return new TransactionOrder()
        {
            TransactionAmount = amountThatCanBeFilledByOrder,
            TransactionPrice = priceToPay,
            OrderId = sellOrder.OrderHolder.Order.Id,
            OrderRemainingAmount = sellOrder.OrderHolder.Order.Amount - amountThatCanBeFilledByOrder,
            OrderOriginalAmount = sellOrder.OrderHolder.Order.Amount,
            OrderPrice = sellOrder.OrderHolder.Order.Price,
            Exchange = sellOrder.Exchange.Id
        };
    }

    /// <summary>
    /// Check if the funds we are trying to use is more than the available funds on the exchange,
    /// if not adjust to the maximum amount we can use
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="sellOrder"></param>
    /// <param name="priceToPay"></param>
    /// <returns></returns>
    private static (bool isPriceAdjusted, decimal priceToUse) AreFundsOnExchangeSufficient(
        Transaction transaction,
        OrderExchangePair sellOrder, decimal priceToPay)
    {
        //Check if there are enough funds on the exchange, else adjust the amount
        if (priceToPay > sellOrder.Exchange.AvailableFunds.Euro)
        {
            return (true, sellOrder.Exchange.AvailableFunds.Euro);
        }

        //We have not used any funds on this exchange yet
        if (!transaction.ExchangePriceUsage.TryGetValue(sellOrder.Exchange.Id, out var exchangePriceUsed))
            return (false, priceToPay);

        //Check if the funds we have used + the funds we are going to use are more than the available funds
        if (exchangePriceUsed + priceToPay >= sellOrder.Exchange.AvailableFunds.Euro)
        {
            return (true, sellOrder.Exchange.AvailableFunds.Euro - exchangePriceUsed);
        }

        return (false, priceToPay);
    }


    /// <summary>
    /// Check if the crypto we are trying to use is more than the available crypto on the exchange,
    /// if not adjust to the maximum amount we can use
    /// </summary>
    /// <param name="transaction"></param>
    /// <param name="sellOrder"></param>
    /// <param name="amountThatCanBeFilledByOrder"></param>
    /// <returns></returns>
    private static (bool isAmountAdjusted, decimal newAmount) AreCryptoOnExchangeSufficient(
        Transaction transaction,
        OrderExchangePair sellOrder, decimal amountThatCanBeFilledByOrder
    )
    {
        
        //Check if there are enough crypto on the exchange, else adjust the amount
        if (amountThatCanBeFilledByOrder > sellOrder.Exchange.AvailableFunds.Crypto)
        {
            return (true, sellOrder.Exchange.AvailableFunds.Crypto);
        }
        
        //We have not used any crypto on this exchange yet
        if (!transaction.ExchangeAmountUsage.TryGetValue(sellOrder.Exchange.Id, out var exchangeAmountUsed))
            return (false, amountThatCanBeFilledByOrder);

        
        //Check if the crypto we have used + the crypto we are going to use are more than the available crypto
        if (exchangeAmountUsed + amountThatCanBeFilledByOrder >= sellOrder.Exchange.AvailableFunds.Crypto)
        {
            return (true, sellOrder.Exchange.AvailableFunds.Crypto - exchangeAmountUsed);
        }

        return (false, amountThatCanBeFilledByOrder);
    }

    private static decimal GetAmountWeCanTakeFromThisOrder(Transaction transaction,
        OrderExchangePair sellOrder)
    {
        return Math.Min(transaction.UnfulfilledAmount, sellOrder.OrderHolder.Order.Amount);
    }
}