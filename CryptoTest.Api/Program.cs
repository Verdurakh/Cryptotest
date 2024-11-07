using System.Text.Json;
using CryptoTest.Models.Enums;
using CryptoTest.Models.OrderBooks;
using CryptoTest.Services.ExchangeData;
using CryptoTest.Services.StrategyService;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

const string pathToExchangeData =
    "exchanges/exchange-01.json,exchanges/exchange-02.json,exchanges/exchange-03.json";


// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddLogging(loggingBuilder => { loggingBuilder.AddConsole(); });

AddDependencyInjection(builder);


var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var isRunningInContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
if (!isRunningInContainer)
    app.UseHttpsRedirection();


AddApis(app);

app.Run();

void AddApis(IEndpointRouteBuilder webApplication)
{
    webApplication.MapGet("/Exchanges",
            (IExchangeService exchangeHolder) => Results.Ok((object?) exchangeHolder.GetExchanges()))
        .WithOpenApi();


    webApplication.MapPost("/Order",
            ([FromQuery] OrderTypeEnum typeEnum, [FromQuery] decimal bitcoins, [FromQuery] decimal price,
                ICryptoTransactionStrategy cryptoTransactionStrategy, IExchangeService exchangeHolder) =>
            {
                if (bitcoins <= 0 || price <= 0)
                {
                    return Results.BadRequest("Bitcoins and price must be positive values.");
                }

                var newOrder = MapRequestToModel(typeEnum, bitcoins, price);

                var exchange = exchangeHolder.GetExchanges();
                var transaction = cryptoTransactionStrategy.CreateTransactionStrategy(exchange, newOrder);
                return Results.Ok(transaction);
            })
        .WithOpenApi();
}

void AddDependencyInjection(WebApplicationBuilder webApplicationBuilder)
{
    webApplicationBuilder.Services.AddScoped<ICryptoTransactionStrategy, CryptoTransactionStrategy>();
    webApplicationBuilder.Services.AddSingleton<IExchangeService>(_ => CreatedLoadedExchangeCache());
}

ExchangeServiceInMemory CreatedLoadedExchangeCache()
{
    var exchangeHolder = new ExchangeServiceInMemory();

    var pathToExchangeDataSplit = pathToExchangeData.Split(',');
    foreach (var exchangeFile in pathToExchangeDataSplit)
    {
        var rawExchangeData = File.ReadAllText(exchangeFile);
        var loadedExchange = JsonSerializer.Deserialize<Exchange>(rawExchangeData);

        if (loadedExchange == null)
            throw new Exception("Exchange data could not be read");

        exchangeHolder.UpdateExchange(loadedExchange);
    }

    return exchangeHolder;
}

Order MapRequestToModel(OrderTypeEnum orderTypeEnum, decimal bitcoins1, decimal price1)
{
    var order = new Order()
    {
        Type = orderTypeEnum.ToString(),
        Id = Guid.NewGuid(),
        Time = DateTime.Now,
        Amount = bitcoins1,
        Price = price1
    };
    return order;
}