using CryptoTest.Models.OrderBooks;
using CryptoTest.Models.Transaction;

namespace CryptoTest.Services;

public interface ICryptoTransactionStrategy
{
    Transaction CreateTransactionStrategy(Exchange exchange, Order order);
}