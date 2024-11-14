using CryptoTest.Models.Enums;
using CryptoTest.Models.OrderBooks;
using CryptoTest.Services.StrategyService;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace CryptoTest.Tests;

public class CryptoStrategyTests
{
    [Fact]
    public void Running_Out_Of_Crypto_On_Exchange_With_Two_Asking()
    {
        //Arrange
        var asking1 = new {price = 2, amount = 2};
        var asking2 = new {price = 2, amount = 2};
        var cryptoOnExchange = 2.5m;
        var orderCrypto = 4;
        var orderPrice = 2;
        var exchange = GetSimpleExchange(asking1.price, asking1.amount, 0, 0, 99999, cryptoOnExchange);
        exchange.OrderBook.Asks.Add(new OrderHolder()
            {Order = new Order() {Amount = asking2.amount, Price = asking2.price}});
        var order = new Order
        {
            Amount = orderCrypto,
            Price = orderPrice,
            Type = OrderTypeEnum.Buy.ToString()
        };
        var cryptoStrategy = CreateCryptoTransactionStrategy();

        //Act
        var transaction = cryptoStrategy.CreateTransactionStrategy(exchange, order);

        //Assert
        transaction.TransactionOrders.Should().HaveCount(2);
        transaction.FullfillmentAmount.Should().Be(cryptoOnExchange);
        transaction.FullfillmentPrice.Should().Be(cryptoOnExchange * orderPrice);
        transaction.UnfulfilledAmount.Should().Be(orderCrypto - cryptoOnExchange);
    }


    [Fact]
    public void Running_Out_Of_Funds_On_Exchange_With_Two_Asking()
    {
        //Arrange
        var exchange = GetSimpleExchange(2, 1, 10, 1, 2.5m, 5);
        exchange.OrderBook.Asks.Add(new OrderHolder() {Order = new Order() {Amount = 1, Price = 2}});
        var order = new Order
        {
            Amount = 2,
            Price = 2,
            Type = OrderTypeEnum.Buy.ToString()
        };
        var cryptoStrategy = CreateCryptoTransactionStrategy();

        //Act
        var transaction = cryptoStrategy.CreateTransactionStrategy(exchange, order);

        //Assert
        transaction.TransactionOrders.Should().HaveCount(2);
        transaction.FullfillmentAmount.Should().Be(1.25m);
        transaction.FullfillmentPrice.Should().Be(2.5m);
        transaction.UnfulfilledAmount.Should().Be(0.75m);
    }

    [Fact]
    public void Running_Out_Of_Funds_On_Exchange()
    {
        //Arrange
        var exchange = GetSimpleExchange(2, 1, 10, 1, 1, 2);
        var order = new Order
        {
            Amount = 1,
            Price = 2,
            Type = OrderTypeEnum.Buy.ToString()
        };
        var cryptoStrategy = CreateCryptoTransactionStrategy();

        //Act
        var transaction = cryptoStrategy.CreateTransactionStrategy(exchange, order);

        //Assert
        transaction.TransactionOrders.Should().HaveCount(1);
        transaction.FullfillmentAmount.Should().Be(0.5m);
        transaction.FullfillmentPrice.Should().Be(1);
        transaction.UnfulfilledAmount.Should().Be(0.5m);
    }


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
    public void Two_Exchange_With_Orders_Every_Other_Exchange_Fulfill_Buy_Order()
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
    
    
    /// <summary>
    /// This one follows the scenario in the example.
    /// lowest price to buy 9 BTC.
    /// we have the following asking
    /// 7 BTC for 3000
    /// 4 BTC for 3300
    /// 9 BTC for 3500
    /// First we buy 7 for 3000
    /// Then we buy 2 for 3300
    /// Result is 7*3000 + 2*3300 = 27600
    /// </summary>
    [Fact]
    public void Buying_Example_Buy_Nine_For_27()
    {
        //Arrange
        var exchanges = new List<Exchange>();
        var exchange1 = GetSimpleExchange(1, 1, 0, 0, 100000.01m, 1000m, "Exchange1");
        exchange1.OrderBook.Asks.Clear();
        exchange1.OrderBook.Asks.Add(new OrderHolder() {Order = new Order() {Amount = 7, Price = 3000}});
        exchange1.OrderBook.Asks.Add(new OrderHolder() {Order = new Order() {Amount = 4, Price = 3300m}});
        exchange1.OrderBook.Asks.Add(new OrderHolder() {Order = new Order() {Amount = 9, Price = 3500m}});
        exchanges.Add(exchange1);

        var order = new Order
        {
            Amount = 9m,
            Price = 100000,
            Type = OrderTypeEnum.Buy.ToString()
        };
        var cryptoStrategy = CreateCryptoTransactionStrategy();

        //Act
        var transaction = cryptoStrategy.CreateTransactionStrategy(exchanges, order);

        //Assert
        transaction.TransactionOrders.Should().HaveCount(2);
        transaction.FullfillmentAmount.Should().Be(9m);
        transaction.FullfillmentPrice.Should().Be(27600m);
        transaction.UnfulfilledAmount.Should().Be(0);
    }
    
    /// <summary>
    /// lowest price to buy 1 BTC.
    /// we have the following asking
    /// 7 BTC for 3000
    /// 4 BTC for 3300
    /// 9 BTC for 3500
    /// First we buy 1 for 3000
    /// Result is 1*3000 = 3000
    /// </summary>
    [Fact]
    public void Buying_Example_Buy_One_For_3()
    {
        //Arrange
        var exchanges = new List<Exchange>();
        var exchange1 = GetSimpleExchange(1, 1, 0, 0, 100000.01m, 1000m, "Exchange1");
        exchange1.OrderBook.Asks.Clear();
        exchange1.OrderBook.Asks.Add(new OrderHolder() {Order = new Order() {Amount = 7, Price = 3000}});
        exchange1.OrderBook.Asks.Add(new OrderHolder() {Order = new Order() {Amount = 4, Price = 3300m}});
        exchange1.OrderBook.Asks.Add(new OrderHolder() {Order = new Order() {Amount = 9, Price = 3500m}});
        exchanges.Add(exchange1);

        var order = new Order
        {
            Amount = 1m,
            Price = 100000,
            Type = OrderTypeEnum.Buy.ToString()
        };
        var cryptoStrategy = CreateCryptoTransactionStrategy();

        //Act
        var transaction = cryptoStrategy.CreateTransactionStrategy(exchanges, order);

        //Assert
        transaction.TransactionOrders.Should().HaveCount(1);
        transaction.FullfillmentAmount.Should().Be(1);
        transaction.FullfillmentPrice.Should().Be(3000m);
        transaction.UnfulfilledAmount.Should().Be(0);
    }
    
    [Fact]
    public void Buying_Try_To_Buy_12_For_3300()
    {
        //Arrange
        var exchanges = new List<Exchange>();
        var exchange1 = GetSimpleExchange(1, 1, 0, 0, 100000.01m, 1000m, "Exchange1");
        exchange1.OrderBook.Asks.Clear();
        exchange1.OrderBook.Asks.Add(new OrderHolder() {Order = new Order() {Amount = 7, Price = 3000}});
        exchange1.OrderBook.Asks.Add(new OrderHolder() {Order = new Order() {Amount = 4, Price = 3300m}});
        exchange1.OrderBook.Asks.Add(new OrderHolder() {Order = new Order() {Amount = 9, Price = 3500m}});
        exchanges.Add(exchange1);

        var order = new Order
        {
            Amount = 12m,
            Price = 3300,
            Type = OrderTypeEnum.Buy.ToString()
        };
        var cryptoStrategy = CreateCryptoTransactionStrategy();

        //Act
        var transaction = cryptoStrategy.CreateTransactionStrategy(exchanges, order);

        //Assert
        transaction.TransactionOrders.Should().HaveCount(2);
        transaction.FullfillmentAmount.Should().Be(11);
        transaction.FullfillmentPrice.Should().Be(34200);
        transaction.UnfulfilledAmount.Should().Be(1);
    }
    
    [Fact]
    public void Buying_Example_Buy_Nine_For_Multiple_Exchange()
    {
        //Arrange
        var exchanges = new List<Exchange>();
        var exchange1 = GetSimpleExchange(1, 1, 0, 0, 100000.01m, 1000m, "Exchange1");
        exchange1.OrderBook.Asks.Clear();
        exchange1.OrderBook.Asks.Add(new OrderHolder() {Order = new Order() {Amount = 7, Price = 3000}});
        exchange1.OrderBook.Asks.Add(new OrderHolder() {Order = new Order() {Amount = 4, Price = 3300m}});
        exchange1.OrderBook.Asks.Add(new OrderHolder() {Order = new Order() {Amount = 9, Price = 3500m}});
        exchanges.Add(exchange1);
        
        var exchange2 = GetSimpleExchange(1, 1, 0, 0, 100000.01m, 1000m, "Exchange2");
        exchange2.OrderBook.Asks.Clear();
        exchange2.OrderBook.Asks.Add(new OrderHolder() {Order = new Order() {Amount = 7, Price = 3000}});
        exchange2.OrderBook.Asks.Add(new OrderHolder() {Order = new Order() {Amount = 4, Price = 3300m}});
        exchange2.OrderBook.Asks.Add(new OrderHolder() {Order = new Order() {Amount = 9, Price = 3500m}});
        exchanges.Add(exchange2);

        var order = new Order
        {
            Amount = 9m,
            Price = 100000,
            Type = OrderTypeEnum.Buy.ToString()
        };
        var cryptoStrategy = CreateCryptoTransactionStrategy();

        //Act
        var transaction = cryptoStrategy.CreateTransactionStrategy(exchanges, order);

        //Assert
        transaction.TransactionOrders.Should().HaveCount(2);
        transaction.FullfillmentAmount.Should().Be(9m);
        transaction.FullfillmentPrice.Should().Be(9*3000);
        transaction.UnfulfilledAmount.Should().Be(0);
    }
    
    [Fact]
    public void Buying_Example_Buy_Nine_For_Multiple_Exchange_With_Limit()
    {
        //Arrange
        var exchanges = new List<Exchange>();
        var exchange1 = GetSimpleExchange(1, 1, 0, 0, 100000.01m, 1m, "Exchange1");
        exchange1.OrderBook.Asks.Clear();
        exchange1.OrderBook.Asks.Add(new OrderHolder() {Order = new Order() {Amount = 7, Price = 3000}});
        exchange1.OrderBook.Asks.Add(new OrderHolder() {Order = new Order() {Amount = 4, Price = 3300m}});
        exchange1.OrderBook.Asks.Add(new OrderHolder() {Order = new Order() {Amount = 9, Price = 3500m}});
        exchanges.Add(exchange1);
        
        var exchange2 = GetSimpleExchange(1, 1, 0, 0, 100000.01m, 1000m, "Exchange2");
        exchange2.OrderBook.Asks.Clear();
        exchange2.OrderBook.Asks.Add(new OrderHolder() {Order = new Order() {Amount = 7, Price = 3000}});
        exchange2.OrderBook.Asks.Add(new OrderHolder() {Order = new Order() {Amount = 4, Price = 3300m}});
        exchange2.OrderBook.Asks.Add(new OrderHolder() {Order = new Order() {Amount = 9, Price = 3500m}});
        exchanges.Add(exchange2);

        var order = new Order
        {
            Amount = 9m,
            Price = 100000,
            Type = OrderTypeEnum.Buy.ToString()
        };
        var cryptoStrategy = CreateCryptoTransactionStrategy();

        //Act
        var transaction = cryptoStrategy.CreateTransactionStrategy(exchanges, order);

        //Assert
        transaction.TransactionOrders.Should().HaveCount(3);
        transaction.FullfillmentAmount.Should().Be(9m);
        transaction.FullfillmentPrice.Should().Be(27300);
        transaction.UnfulfilledAmount.Should().Be(0);
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