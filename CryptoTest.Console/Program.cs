using System.Text.Json;
using CryptoTest.Models;
using CryptoTest.Models.OrderBooks;
using CryptoTest.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddScoped<ICryptoTransactionStrategy, CryptoTransactionStrategy>();
        services.AddSingleton<ExchangeHolder>();
    })
    .Build();


const string pathToExchangeData = "exchange-01.json";


var exchangeHolder = host.Services.GetRequiredService<ExchangeHolder>();

try
{
    var rawExchangeData = File.ReadAllText(pathToExchangeData);
    var loadedExchange = JsonSerializer.Deserialize<Exchange>(rawExchangeData);

    if (loadedExchange == null)
        throw new Exception("Exchange data could not be read");

    exchangeHolder.UpdateExchange(loadedExchange);
}
catch (Exception)
{
    Console.WriteLine("Unable to read exchange data");
    Console.WriteLine("Exiting");
    return;
}

var exchange = exchangeHolder.GetExchange();

Console.WriteLine($"Exchange data '{exchange.Id}' loaded");

var filteredExchange = CryptoStrategyFilterExchangeLimit.FilterExchangeLimit(exchange);
var sortedExchange = CryptoStrategySorting.SortExchangeOrders(filteredExchange);

Console.WriteLine($"Exchange data funds: {exchange.AvailableFunds.Euro} Euro, {exchange.AvailableFunds.Crypto} Crypto");

Console.WriteLine($"Lowest asking price: {sortedExchange.OrderBook.Asks.First().Order.Price}");
Console.WriteLine($"Highest bidding price: {sortedExchange.OrderBook.Bids.First().Order.Price}");

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

var cryptoBuildingStrategy = host.Services.GetRequiredService<ICryptoTransactionStrategy>();

var transaction = cryptoBuildingStrategy.CreateTransactionStrategy(sortedExchange, newOrder);
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
    Console.WriteLine(
        $"Unfulfilled amount: {transaction.UnfulfilledAmount} of asking amount {newOrder.Amount} , diff of {newOrder.Amount - transaction.UnfulfilledAmount}");

Console.WriteLine("Finished");