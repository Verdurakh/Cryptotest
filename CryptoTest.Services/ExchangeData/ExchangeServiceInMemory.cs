using CryptoTest.Models.OrderBooks;
using Microsoft.Extensions.Caching.Memory;

namespace CryptoTest.Services.ExchangeData;

public class ExchangeServiceInMemory : IExchangeService
{
    private readonly MemoryCache _exchangeCache = new(new MemoryCacheOptions());
    private static readonly List<string> ExchangeIds = [];

    public Exchange? GetExchange()
    {
        return _exchangeCache.Get<Exchange>(ExchangeIds.FirstOrDefault() ?? string.Empty);
    }

    public IEnumerable<Exchange> GetExchanges()
    {
        var exchanges = new List<Exchange>();
        foreach (var exchangeId in ExchangeIds)
        {
            if (_exchangeCache.TryGetValue(exchangeId, out Exchange? exchange) && exchange != null)
                exchanges.Add(exchange);
        }

        return exchanges;
    }

    public void UpdateExchange(Exchange exchange)
    {
        _exchangeCache.Set(exchange.Id, exchange);
        ExchangeIds.Add(exchange.Id);
    }
}