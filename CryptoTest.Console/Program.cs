using System.Text.Json;
using CryptoTest.Models.Enums;
using CryptoTest.Models.OrderBooks;
using CryptoTest.Services.ExchangeData;
using CryptoTest.Services.StrategyService;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

const string exitCode = "4";
const string exchangeCode = "3";
const string pathToExchangeData =
    "exchanges/exchange-01.json,exchanges/exchange-02.json,exchanges/exchange-03.json";


using var host = SetupDependencyInjection(args);
var exchangeHolder = host.Services.GetRequiredService<IExchangeService>();
var exchanges = exchangeHolder.GetExchanges();
PrintExchangeData(exchanges);

while (true)
{
    PrintMenu();
    var input = Console.ReadLine();
    if (input == exitCode)
        break;

    if (input == exchangeCode)
    {
        PrintExchangeData(exchanges);
        continue;
    }

    if (!Enum.TryParse<OrderTypeEnum>(input, out var orderType) || !Enum.IsDefined(typeof(OrderTypeEnum), orderType))
    {
        Console.WriteLine("Invalid input");
        continue;
    }

    var newOrder = new Order()
    {
        Type = orderType.ToString(),
        Id = Guid.NewGuid(),
        Time = DateTime.Now,
    };

    newOrder.Amount = GetPositiveDecimal($"Input amount of btc to {newOrder.Type}");
    newOrder.Price = GetPositiveDecimal("Input price per btc");

    RunOrderOnMultipleExchanges(exchangeHolder, host, newOrder);
}

decimal GetPositiveDecimal(string prompt)
{
    decimal value;
    while (true)
    {
        Console.WriteLine(prompt);
        if (decimal.TryParse(Console.ReadLine(), out value) && value > 0)
            return value;

        Console.WriteLine("Invalid input");
    }
}


void PrintMenu()
{
    Console.WriteLine("Choose an option:");
    Console.WriteLine($"{(int) OrderTypeEnum.Buy}: Buy");
    Console.WriteLine($"{(int) OrderTypeEnum.Sell}: Sell");
    Console.WriteLine($"{exchangeCode}: Show exchange data");
    Console.WriteLine($"{exitCode}: Exit");
}

IHost SetupDependencyInjection(string[] strings)
{
    IHost? host2 = null;
    try
    {
        host2 = Host.CreateDefaultBuilder(strings)
            .ConfigureLogging(logger =>
            {
                logger.ClearProviders();
                //logger.AddConsole();
            })
            .ConfigureServices((_, services) =>
            {
                services.AddScoped<ICryptoTransactionStrategy, CryptoTransactionStrategy>();
                services.AddSingleton<IExchangeService>(_ =>
                {
                    var exchangeCache = new ExchangeServiceInMemory();


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

void RunOrderOnMultipleExchanges(IExchangeService exchangeHolder1, IHost host1, Order newOrder)
{
    var exchanges = exchangeHolder1.GetExchanges();
    Console.WriteLine();
    Console.WriteLine();

    Console.WriteLine($"Order to {newOrder.Type}: {newOrder.Amount} btc for {newOrder.Price} per btc");
    Console.WriteLine();
    var cryptoBuildingStrategy = host1.Services.GetRequiredService<ICryptoTransactionStrategy>();

    var transaction = cryptoBuildingStrategy.CreateTransactionStrategy(exchanges, newOrder);

    foreach (var transactionTransactionOrder in transaction.TransactionOrders)
    {
        Console.WriteLine(
            $"Order to use: {transactionTransactionOrder.OrderId}, using:{transactionTransactionOrder.TransactionAmount} btc. remaining on order: {transactionTransactionOrder.OrderRemainingAmount} / {transactionTransactionOrder.OrderOriginalAmount}, total price {transactionTransactionOrder.TransactionPrice}, price per unit {transactionTransactionOrder.OrderPrice}, Exchange: {transactionTransactionOrder.Exchange}");
    }

    Console.WriteLine();
    Console.WriteLine(
        $"Transaction we got: {transaction.FullfillmentAmount} btc for {transaction.FullfillmentPrice} eur");

    Console.WriteLine();
    if (transaction.TransactionOrders.Count == 0)
        Console.WriteLine($"There was not any orders that could be used for the transaction");

    if (transaction.UnfulfilledAmount > 0)
        Console.WriteLine(
            $"There was some btc that we could not get: {transaction.UnfulfilledAmount} / {newOrder.Amount} , diff of {newOrder.Amount - transaction.UnfulfilledAmount}");

    Console.WriteLine();
    Console.WriteLine("Finished");
    Console.WriteLine();
}

void PrintExchangeData(IEnumerable<Exchange> enumerable)
{
    var totalEuro = 0m;
    var totalCrypto = 0m;
    foreach (var exchange in enumerable)
    {
        Console.WriteLine($"Exchange data '{exchange.Id}' loaded");
        Console.WriteLine(
            $"Exchange data funds: {exchange.AvailableFunds.Euro} Euro, {exchange.AvailableFunds.Crypto} Crypto");

        Console.WriteLine(
            $"Lowest asking price: {exchange.OrderBook.Asks.OrderBy(ask => ask.Order.Price).First().Order.Price}");
        Console.WriteLine(
            $"Highest bidding price: {exchange.OrderBook.Bids.OrderByDescending(bid => bid.Order.Price).First().Order.Price}");
        Console.WriteLine();
        totalEuro += exchange.AvailableFunds.Euro;
        totalCrypto += exchange.AvailableFunds.Crypto;
    }
    
    Console.WriteLine($"Total funds: {totalEuro} Euro, {totalCrypto} Crypto");
    Console.WriteLine();
}