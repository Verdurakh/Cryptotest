using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using CryptoTest.Models.OrderBooks;
using CryptoTest.Services;
using CryptoTest.Services.ExchangeData;
using CryptoTest.Services.StrategyService;

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

void AddApis(WebApplication webApplication)
{
    webApplication.MapGet("/Exchanges",
            (IExchangeService exchangeHolder) => Results.Ok((object?) exchangeHolder.GetExchanges()))
        .WithOpenApi();


    webApplication.MapPost("/Order",
            (Order order, ICryptoTransactionStrategy cryptoTransactionStrategy, IExchangeService exchangeHolder) =>
            {
                var validationResults = new List<ValidationResult>();
                if (!Validator.TryValidateObject(order, new ValidationContext(order), validationResults, true))
                {
                    return Results.BadRequest(validationResults);
                }

                var exchange = exchangeHolder.GetExchanges();
                var transaction = cryptoTransactionStrategy.CreateTransactionStrategy(exchange, order);
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