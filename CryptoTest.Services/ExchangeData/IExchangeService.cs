using CryptoTest.Models.OrderBooks;

namespace CryptoTest.Services.ExchangeData;

public interface IExchangeService
{
    IEnumerable<Exchange> GetExchanges();
    void UpdateExchange(Exchange exchange);
}