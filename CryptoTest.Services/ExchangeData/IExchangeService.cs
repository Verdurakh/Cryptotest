using CryptoTest.Models.OrderBooks;

namespace CryptoTest.Services;

public interface IExchangeService
{
    Exchange? GetExchange();
    IEnumerable<Exchange> GetExchanges();
    void UpdateExchange(Exchange exchange);
}