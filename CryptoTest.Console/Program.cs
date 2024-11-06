using System.Text.Json;
using CryptoTest.Models;
using CryptoTest.Models.OrderBooks;
using CryptoTest.Services;

Console.WriteLine("Hello, World!");


Console.WriteLine("Read exchange data from file");
var rawExchangeData = File.ReadAllText("exchange-01.json");
var exchange = JsonSerializer.Deserialize<Exchange>(rawExchangeData);
if (exchange == null)
    throw new Exception("Exchange data could not be read");

Console.WriteLine("Exchange data read");

var newExchange = CryptoStrategyFilterExchangeLimit.FilterExchangeLimit(exchange);
Console.WriteLine("Restricted sell orders:");

var sortedExchange = CryptoStrategySorting.SortExchangeOrders(newExchange);

Console.WriteLine("Sorted buy orders:");

Console.WriteLine($"Exchange data: {exchange.AvailableFunds.Euro} Euro, {exchange.AvailableFunds.Crypto} Crypto");

var newOrder = new Order()
{
    Amount = 0.903m,
    Price = 52302.73m,
    Type = OrderTypeEnum.Sell.ToString(),
    Id = Guid.NewGuid(),
    Time = DateTime.Now,
    Kind = "Limit"
};

Console.WriteLine($"Order to {newOrder.Type}: {newOrder.Amount} for {newOrder.Price}");

var transaction = CryptoBuyingStrategy.CreateTransactionStrategy(sortedExchange, newOrder);
Console.WriteLine(
    $"Transaction: {transaction.FullfillmentAmount} for {transaction.FullfillmentPrice}, unfulfilled: {transaction.UnfulfilledAmount}");
foreach (var transactionTransactionOrder in transaction.TransactionOrders)
{
    Console.WriteLine(
        $"Order: {transactionTransactionOrder.OrderId}, remaining: {transactionTransactionOrder.OrderRemainingAmount} / {transactionTransactionOrder.OrderOriginalAmount}, total price {transactionTransactionOrder.TransactionPrice}, price per unit {transactionTransactionOrder.OrderPrice}");
}

if (transaction.TransactionOrders.Count == 0)
    Console.WriteLine($"Unable to fill any asking transaction");

if (transaction.UnfulfilledAmount > 0)
    Console.WriteLine($"Unfulfilled amount: {transaction.UnfulfilledAmount} of asking amount {newOrder.Amount} , diff of {newOrder.Amount - transaction.UnfulfilledAmount}");