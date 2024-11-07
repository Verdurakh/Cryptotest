using CryptoTest.Models.OrderBooks;

namespace CryptoTest.Services.ExchangeData;

public interface IExchangeService
{
    Exchange? GetExchange();
    IEnumerable<Exchange> GetExchanges();
    void UpdateExchange(Exchange exchange);
}