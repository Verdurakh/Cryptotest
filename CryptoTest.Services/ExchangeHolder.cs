using CryptoTest.Models.OrderBooks;
using Microsoft.Extensions.Caching.Memory;

namespace CryptoTest.Services;

public class ExchangeHolder
{
    private readonly MemoryCache _exchangeCache = new(new MemoryCacheOptions());
    private const string ExchangeKey = "Exchange";

    public Exchange? GetExchange()
    {
        return _exchangeCache.Get<Exchange>(ExchangeKey);
    }

    public void UpdateExchange(Exchange exchange)
    {
        _exchangeCache.Set(ExchangeKey, exchange);
    }
}