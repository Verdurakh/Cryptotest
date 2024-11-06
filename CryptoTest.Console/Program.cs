using System.Text.Json;
using CryptoTest.Models;
using CryptoTest.Models.OrderBooks;
using CryptoTest.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using var host = SetupDependencyInjection(args);
var exchangeHolder = host.Services.GetRequiredService<ExchangeHolder>();
var exchanges = exchangeHolder.GetExchanges();
foreach (var exchange in exchanges)
{
    Console.WriteLine($"Exchange data '{exchange.Id}' loaded");
    Console.WriteLine(
        $"Exchange data funds: {exchange.AvailableFunds.Euro} Euro, {exchange.AvailableFunds.Crypto} Crypto");

    Console.WriteLine($"Lowest asking price: {exchange.OrderBook.Asks.First().Order.Price}");
    Console.WriteLine($"Highest bidding price: {exchange.OrderBook.Bids.First().Order.Price}");
    Console.WriteLine();
}

while (true)
{
    PrintMenu();
    var input = Console.ReadLine();
    if (input == "3")
        break;
    if (input != "1" && input != "2")
    {
        Console.WriteLine("Invalid input");
        continue;
    }

    var newOrder = new Order()
    {
        Amount = 20.903m,
        Price = 58302.73m,
        Type = input == "1" ? OrderTypeEnum.Buy.ToString() : OrderTypeEnum.Sell.ToString(),
        Id = Guid.NewGuid(),
        Time = DateTime.Now,
        Kind = "Limit"
    };
    do
    {
        Console.WriteLine($"Input amount of btc to {newOrder.Type}");
        ;
        if (decimal.TryParse(Console.ReadLine(), out var value))
        {
            newOrder.Amount = value;
            break;
        }

        Console.WriteLine("Invalid input");
    } while (true);


    do
    {
        Console.WriteLine("Input price per btc");
        if (decimal.TryParse(Console.ReadLine(), out var value))
        {
            newOrder.Price = value;
            break;
        }

        Console.WriteLine("Invalid input");
    } while (true);

    RunOrderOnMultipleExchanges(exchangeHolder, host, newOrder);
}


// var newOrder = new Order()
// {
//     Amount = 20.903m,
//     Price = 58302.73m,
//     Type = OrderTypeEnum.Buy.ToString(),
//     Id = Guid.NewGuid(),
//     Time = DateTime.Now,
//     Kind = "Limit"
// };


//RunOrderOnSingleExchange();

void PrintMenu()
{
    Console.WriteLine("1. Buy");
    Console.WriteLine("2. Sell");
    Console.WriteLine("3. Exit");
}

IHost SetupDependencyInjection(string[] strings)
{
    IHost? host2 = null;
    try
    {
        host2 = Host.CreateDefaultBuilder(strings)
            .ConfigureServices((_, services) =>
            {
                services.AddScoped<ICryptoTransactionStrategy, CryptoTransactionStrategy>();
                services.AddSingleton<ExchangeHolder>(_ =>
                {
                    var exchangeCache = new ExchangeHolder();

                    const string pathToExchangeData =
                        "exchanges/exchange-01.json,exchanges/exchange-02.json,exchanges/exchange-03.json";

                    var pathToExchangeDataSplit = pathToExchangeData.Split(',');
                    foreach (var exchangeFile in pathToExchangeDataSplit)
                    {
                        var rawExchangeData = File.ReadAllText(exchangeFile);
                        var loadedExchange = JsonSerializer.Deserialize<Exchange>(rawExchangeData);

                        if (loadedExchange == null)
                            throw new Exception("Exchange data could not be read");

                        exchangeCache.UpdateExchange(loadedExchange);
                    }


                    return exchangeCache;
                });
            })
            .Build();
        return host2;
    }
    catch
    {
        host2?.Dispose();
        throw;
    }
}

void RunOrderOnMultipleExchanges(ExchangeHolder exchangeHolder1, IHost host1, Order newOrder)
{
    var exchanges = exchangeHolder1.GetExchanges();


    Console.WriteLine($"Order to {newOrder.Type}: {newOrder.Amount} for {newOrder.Price}");
    Console.WriteLine();
    var cryptoBuildingStrategy = host1.Services.GetRequiredService<ICryptoTransactionStrategy>();

    var transaction = cryptoBuildingStrategy.CreateTransactionStrategy(exchanges, newOrder);
    Console.WriteLine(
        $"Transaction: {transaction.FullfillmentAmount} btc for {transaction.FullfillmentPrice} eur, unfulfilled: {transaction.UnfulfilledAmount}");
    foreach (var transactionTransactionOrder in transaction.TransactionOrders)
    {
        Console.WriteLine(
            $"Order: {transactionTransactionOrder.OrderId}, amount of btc:{transactionTransactionOrder.TransactionAmount}. remaining: {transactionTransactionOrder.OrderRemainingAmount} / {transactionTransactionOrder.OrderOriginalAmount}, total price {transactionTransactionOrder.TransactionPrice}, price per unit {transactionTransactionOrder.OrderPrice}, Exchange: {transactionTransactionOrder.Exchange}");
    }

    Console.WriteLine();
    if (transaction.TransactionOrders.Count == 0)
        Console.WriteLine($"Unable to fill any asking transaction");

    if (transaction.UnfulfilledAmount > 0)
        Console.WriteLine(
            $"Unfulfilled amount: {transaction.UnfulfilledAmount} of asking amount {newOrder.Amount} , diff of {newOrder.Amount - transaction.UnfulfilledAmount}");

    Console.WriteLine("Finished");
}