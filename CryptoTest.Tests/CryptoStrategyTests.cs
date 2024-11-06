using CryptoTest.Models;
using CryptoTest.Models.OrderBooks;
using CryptoTest.Services;

namespace CryptoTest.Tests;

public class CryptoStrategyTests
{
    [Fact]
    public void Exchange_With_Orders_Can_Not_Fulfill_Buy_Order()
    {
        //Arrange
        var exchange = GetSimpleExchange(5, 1, 0, 0);
        var order = new Order
        {
            Amount = 1,
            Price = 4,
            Type = OrderTypeEnum.Buy.ToString()
        };
        var cryptoStrategy = new CryptoTransactionStrategy();
        
        //Act
        var transaction = cryptoStrategy.CreateTransactionStrategy(exchange, order);

        //Assert
        Assert.Empty(transaction.TransactionOrders);
        Assert.Equal(0, transaction.FullfillmentAmount);
        Assert.Equal(0, transaction.FullfillmentPrice);
        Assert.Equal(1, transaction.UnfulfilledAmount);
    }
    
    
    [Fact]
    public void Exchange_With_Orders_Can_Fulfill_Buy_Order()
    {
        //Arrange
        var exchange = GetSimpleExchange(5, 1, 0, 0);
        var order = new Order
        {
            Amount = 1,
            Price = 5,
            Type = OrderTypeEnum.Buy.ToString()
        };
        var cryptoStrategy = new CryptoTransactionStrategy();

        //Act
        var transaction = cryptoStrategy.CreateTransactionStrategy(exchange, order);

        //Assert
        Assert.Single(transaction.TransactionOrders);
        Assert.Equal(1, transaction.FullfillmentAmount);
        Assert.Equal(5, transaction.FullfillmentPrice);
        Assert.Equal(0, transaction.UnfulfilledAmount);
    }

    [Fact]
    public void Exchange_With_Orders_Can_Partially_Fulfill_Buy_Order()
    {
        //Arrange
        var exchange = GetSimpleExchange(5, 1, 0, 0);
        var order = new Order
        {
            Amount = 1.5m,
            Price = 5,
            Type = OrderTypeEnum.Buy.ToString()
        };
        var cryptoStrategy = new CryptoTransactionStrategy();

        //Act
        var transaction = cryptoStrategy.CreateTransactionStrategy(exchange, order);

        //Assert
        Assert.Single(transaction.TransactionOrders);
        Assert.Equal(1, transaction.FullfillmentAmount);
        Assert.Equal(5, transaction.FullfillmentPrice);
        Assert.Equal(0.5m, transaction.UnfulfilledAmount);
    }

    private Exchange GetSimpleExchange(decimal askingprice, decimal askingAmount, decimal bidprice, decimal bidAmount)
    {
        var exchange = new Exchange()
        {
            Id = Guid.NewGuid().ToString(),
            AvailableFunds = new AvailableFunds
            {
                Euro = 1000,
                Crypto = 1000
            },
            OrderBook = new OrderBook
            {
                Asks =
                [
                    new OrderHolder
                    {
                        Order = new()
                        {
                            Type = OrderTypeEnum.Sell.ToString(),
                            Amount = askingAmount,
                            Price = askingprice
                        }
                    }
                ],
                Bids =
                [
                    new OrderHolder
                    {
                        Order = new Order
                        {
                            Type = OrderTypeEnum.Buy.ToString(),
                            Amount = bidAmount,
                            Price = bidprice
                        }
                    }
                ]
            }
        };

        exchange = CryptoStrategyFilterExchangeLimit.FilterExchangeLimit(exchange);
        exchange = CryptoStrategySorting.SortExchangeOrders(exchange);
        return exchange;
    }
}