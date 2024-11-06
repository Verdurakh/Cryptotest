using System.Text.Json;
using CryptoTest.Models.OrderBooks;
using CryptoTest.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<ICryptoTransactionStrategy, CryptoTransactionStrategy>();
builder.Services.AddSingleton<ExchangeHolder>(_ =>
{
    var exchangeCache = new ExchangeHolder();

    const string pathToExchangeData = "exchange-01.json,exchange-02.json";

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


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.MapGet("/Exchange", (ExchangeHolder exchangeHolder) => { return exchangeHolder.GetExchange(); })
    .WithOpenApi();


app.MapPost("/Order",
        (Order order, ICryptoTransactionStrategy cryptoTransactionStrategy, ExchangeHolder exchangeHolder) =>
        {
            var exchange = exchangeHolder.GetExchange();
            var transaction = cryptoTransactionStrategy.CreateTransactionStrategy(exchange, order);
            return transaction;
        })
    .WithOpenApi();

app.Run();