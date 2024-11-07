using CryptoTest.Models;
using CryptoTest.Models.Enums;
using CryptoTest.Models.OrderBooks;
using CryptoTest.Services;
using CryptoTest.Services.StrategyService;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace CryptoTest.Tests;

public class CryptoStrategyTests
{
    [Fact]
    public void Empty_On_Crypto_Exchange_Cannot_Fulfill_Order()
    {
        //Arrange
        var exchange = GetSimpleExchange(5, 1, 10, 1, 10, 0);
        var order = new Order
        {
            Amount = 1,
            Price = 4,
            Type = OrderTypeEnum.Sell.ToString()
        };
        var cryptoStrategy = CreateCryptoTransactionStrategy();

        //Act
        var transaction = cryptoStrategy.CreateTransactionStrategy(exchange, order);

        //Assert
        transaction.TransactionOrders.Should().HaveCount(0);
        transaction.FullfillmentAmount.Should().Be(0);
        transaction.FullfillmentPrice.Should().Be(0);
        transaction.UnfulfilledAmount.Should().Be(1);
    }


    [Fact]
    public void Empty_On_Euro_Exchange_Cannot_Fulfill_Order()
    {
        //Arrange
        var exchange = GetSimpleExchange(5, 1, 10, 1, 0, 10);
        var order = new Order
        {
            Amount = 1,
            Price = 4,
            Type = OrderTypeEnum.Sell.ToString()
        };
        var cryptoStrategy = CreateCryptoTransactionStrategy();

        //Act
        var transaction = cryptoStrategy.CreateTransactionStrategy(exchange, order);

        //Assert
        transaction.TransactionOrders.Should().HaveCount(0);
        transaction.FullfillmentAmount.Should().Be(0);
        transaction.FullfillmentPrice.Should().Be(0);
        transaction.UnfulfilledAmount.Should().Be(1);
    }

    [Fact]
    public void Empty_Exchange_Cannot_Fulfill_Order()
    {
        //Arrange
        var exchange = GetSimpleExchange(5, 1, 10, 1, 0, 0);
        var order = new Order
        {
            Amount = 1,
            Price = 4,
            Type = OrderTypeEnum.Sell.ToString()
        };
        var cryptoStrategy = CreateCryptoTransactionStrategy();

        //Act
        var transaction = cryptoStrategy.CreateTransactionStrategy(exchange, order);

        //Assert
        transaction.TransactionOrders.Should().HaveCount(0);
        transaction.FullfillmentAmount.Should().Be(0);
        transaction.FullfillmentPrice.Should().Be(0);
        transaction.UnfulfilledAmount.Should().Be(1);
    }


    [Fact]
    public void Exchange_With_Orders_Can_Fulfill_Sell_Order_Get_Better_Than_Askin_Price()
    {
        //Arrange
        var exchange = GetSimpleExchange(5, 1, 10, 1);
        var order = new Order
        {
            Amount = 1,
            Price = 4,
            Type = OrderTypeEnum.Sell.ToString()
        };
        var cryptoStrategy = CreateCryptoTransactionStrategy();

        //Act
        var transaction = cryptoStrategy.CreateTransactionStrategy(exchange, order);

        //Assert
        transaction.TransactionOrders.Should().HaveCount(1);
        transaction.FullfillmentAmount.Should().Be(1);
        transaction.FullfillmentPrice.Should().Be(10);
        transaction.UnfulfilledAmount.Should().Be(0);
    }


    [Fact]
    public void Exchange_With_Orders_Can_Fulfill_Sell_Order()
    {
        //Arrange
        var exchange = GetSimpleExchange(5, 1, 4, 1);
        var order = new Order
        {
            Amount = 1,
            Price = 4,
            Type = OrderTypeEnum.Sell.ToString()
        };
        var cryptoStrategy = CreateCryptoTransactionStrategy();

        //Act
        var transaction = cryptoStrategy.CreateTransactionStrategy(exchange, order);

        //Assert
        transaction.TransactionOrders.Should().HaveCount(1);
        transaction.FullfillmentAmount.Should().Be(1);
        transaction.FullfillmentPrice.Should().Be(4);
        transaction.UnfulfilledAmount.Should().Be(0);
    }


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
        var cryptoStrategy = CreateCryptoTransactionStrategy();

        //Act
        var transaction = cryptoStrategy.CreateTransactionStrategy(exchange, order);

        //Assert
        transaction.TransactionOrders.Should().BeEmpty();
        transaction.FullfillmentAmount.Should().Be(0);
        transaction.FullfillmentPrice.Should().Be(0);
        transaction.UnfulfilledAmount.Should().Be(1);
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
        var cryptoStrategy = CreateCryptoTransactionStrategy();

        //Act
        var transaction = cryptoStrategy.CreateTransactionStrategy(exchange, order);

        //Assert
        transaction.TransactionOrders.Should().HaveCount(1);
        transaction.FullfillmentAmount.Should().Be(1);
        transaction.FullfillmentPrice.Should().Be(5);
        transaction.UnfulfilledAmount.Should().Be(0);
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
        var cryptoStrategy = CreateCryptoTransactionStrategy();

        //Act
        var transaction = cryptoStrategy.CreateTransactionStrategy(exchange, order);

        //Assert
        transaction.TransactionOrders.Should().HaveCount(1);
        transaction.FullfillmentAmount.Should().Be(1);
        transaction.FullfillmentPrice.Should().Be(5);
        transaction.UnfulfilledAmount.Should().Be(0.5m);
    }

    [Fact]
    public void Two_Exchange_With_Orders_Can_Fulfill_Buy_Prioritize_Cheaper_Second_Exchange_Order()
    {
        //Arrange
        var exchanges = new List<Exchange>();
        exchanges.Add(GetSimpleExchange(3, 5, 0, 0, 1000, 5, "Exchange1"));
        exchanges.Add(GetSimpleExchange(1, 5, 0, 0, 1000, 2, "Exchange2"));
        var order = new Order
        {
            Amount = 1.5m,
            Price = 5,
            Type = OrderTypeEnum.Buy.ToString()
        };
        var cryptoStrategy = CreateCryptoTransactionStrategy();

        //Act
        var transaction = cryptoStrategy.CreateTransactionStrategy(exchanges, order);

        //Assert
        transaction.TransactionOrders.Should().HaveCount(1);
        transaction.TransactionOrders.Should().OnlyContain(w => w.Exchange.Equals("Exchange2"));
        transaction.FullfillmentAmount.Should().Be(1.5m);
        transaction.FullfillmentPrice.Should().Be(1 * 1.5m);
        transaction.UnfulfilledAmount.Should().Be(0.0m);
    }

    [Fact]
    public void Two_Exchange_With_Orders_Can_Fulfill_Buy_Prioritize_Cheaper_Exchange_Order()
    {
        //Arrange
        var exchanges = new List<Exchange>();
        exchanges.Add(GetSimpleExchange(3, 5, 0, 0, 1000, 5, "Exchange1"));
        exchanges.Add(GetSimpleExchange(5, 1, 0, 0, 1000, 2, "Exchange2"));
        var order = new Order
        {
            Amount = 1.5m,
            Price = 5,
            Type = OrderTypeEnum.Buy.ToString()
        };
        var cryptoStrategy = CreateCryptoTransactionStrategy();

        //Act
        var transaction = cryptoStrategy.CreateTransactionStrategy(exchanges, order);

        //Assert
        transaction.TransactionOrders.Should().HaveCount(1);
        transaction.TransactionOrders.Should().OnlyContain(w => w.Exchange.Equals("Exchange1"));
        transaction.FullfillmentAmount.Should().Be(1.5m);
        transaction.FullfillmentPrice.Should().Be(3 * 1.5m);
        transaction.UnfulfilledAmount.Should().Be(0.0m);
    }

    [Fact]
    public void Two_Exchange_With_Orders_Can_Fulfill_Buy_Order()
    {
        //Arrange
        var exchanges = new List<Exchange>();
        exchanges.Add(GetSimpleExchange(5, 1, 0, 0, 1000, 1, "Exchange1"));
        exchanges.Add(GetSimpleExchange(5, 1, 0, 0, 1000, 2, "Exchange2"));
        var order = new Order
        {
            Amount = 1.5m,
            Price = 5,
            Type = OrderTypeEnum.Buy.ToString()
        };
        var cryptoStrategy = CreateCryptoTransactionStrategy();

        //Act
        var transaction = cryptoStrategy.CreateTransactionStrategy(exchanges, order);

        //Assert
        transaction.TransactionOrders.Should().HaveCount(2);
        transaction.FullfillmentAmount.Should().Be(1.5m);
        transaction.FullfillmentPrice.Should().Be(5 * 1.5m);
        transaction.UnfulfilledAmount.Should().Be(0.0m);
    }

    [Fact]
    public void Two_Exchange_With_Orders_Runs_Out_Of_Crypto_Can_Partially_Fulfill_Buy_Order()
    {
        //Arrange
        var exchanges = new List<Exchange>();
        exchanges.Add(GetSimpleExchange(5, 1, 0, 0, 1000, 1, "Exchange1"));
        exchanges.Add(GetSimpleExchange(5, 1, 0, 0, 1000, 0.1m, "Exchange2"));
        var order = new Order
        {
            Amount = 1.5m,
            Price = 5,
            Type = OrderTypeEnum.Buy.ToString()
        };
        var cryptoStrategy = CreateCryptoTransactionStrategy();

        //Act
        var transaction = cryptoStrategy.CreateTransactionStrategy(exchanges, order);

        //Assert
        transaction.TransactionOrders.Should().HaveCount(2);
        transaction.FullfillmentAmount.Should().Be(1.1m);
        transaction.FullfillmentPrice.Should().Be(5 * 1.1m);
        transaction.UnfulfilledAmount.Should().Be(0.4m);
    }

    [Fact]
    public void Two_Exchange_With_Orders_Every_Other_Exchange_fillment_Buy_Order()
    {
        //Arrange
        var exchanges = new List<Exchange>();
        var exchange1 = GetSimpleExchange(1, 1, 0, 0, 1000, 10, "Exchange1");
        exchange1.OrderBook.Asks.Add(new OrderHolder() {Order = new Order() {Amount = 1, Price = 3}});
        exchanges.Add(exchange1);
        var exchange2 = GetSimpleExchange(2, 1, 0, 0, 1000, 10, "Exchange2");
        exchange2.OrderBook.Asks.Add(new OrderHolder() {Order = new Order() {Amount = 1, Price = 4}});
        exchanges.Add(exchange2);
        var order = new Order
        {
            Amount = 5m,
            Price = 6,
            Type = OrderTypeEnum.Buy.ToString()
        };
        var cryptoStrategy = CreateCryptoTransactionStrategy();

        //Act
        var transaction = cryptoStrategy.CreateTransactionStrategy(exchanges, order);

        //Assert
        transaction.TransactionOrders.Should().HaveCount(4);
        transaction.FullfillmentAmount.Should().Be(4m);
        transaction.FullfillmentPrice.Should().Be(10);
        transaction.UnfulfilledAmount.Should().Be(1m);
    }

    private static ICryptoTransactionStrategy CreateCryptoTransactionStrategy()
    {
        return new CryptoTransactionStrategy(new Mock<ILogger<CryptoTransactionStrategy>>().Object);
    }

    private Exchange GetSimpleExchange(decimal askingprice, decimal askingAmount, decimal bidprice, decimal bidAmount,
        decimal euro = 1000, decimal crypto = 1000, string name = "Exchange")
    {
        var exchange = new Exchange()
        {
            Id = name,
            AvailableFunds = new AvailableFunds
            {
                Euro = euro,
                Crypto = crypto
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

        return exchange;
    }
}