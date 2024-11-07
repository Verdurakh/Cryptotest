- Part 1 -
  Your task is to implement a meta-exchange that always gives the user the best possible price if he is
  buying or selling a certain amount of BTC. Technically, you will be given n order books [from n different
  cryptoexchanges], the type of order (buy or sell), and the amount of BTC that our user wants to buy or
  sell. Your algorithm needs to output one or more buy or sell orders.
  In effect, our user buys the specified amount of BTC for the lowest possible price or sells the specified
  amount of BTC for the highest possible price.
  To make life a bit more complicated, each cryptoexchange has EUR and BTC balance. Your algorithm
  needs to achieve the best price within these constraints. The algorithm cannot transfer any money or
  crypto between cryptoexchanges, that means you can only sell what you have ‘stored’ on that
  cryptoexchange account (EUR or BTC).
  Together with this task, you will receive a bunch of JSON files with order books you can use to test your
  solution. In each file, you will also find the given limit (EUR/BTC) of this cryptoexchange.
  Your solution should be a relatively simple .NET Core console-mode application, which reads the order
  books with limits, order amounts and order type, and outputs a set of orders to execute against the given
  order books (exchanges).
- Part 2 -
  Implement a Web service (Kestrel, .NET Core API), and expose the implemented functionality through it.
  Implement an endpoint that will receive the required parameters (order amount, order type) and return
  a JSON response with the "best execution" plan.
  BONUS TASKS
- Write some tests, on relatively simple input data (e.g., order books with only a few bids and asks), to
  test your solution on typical and edge cases.
- Deploy your Web service locally with Docker.

# How to run the project

- Clone the project
- Open the project in your editor
- Run the console application
- Or run the web api project
    - Or run the docker container
- Exchange data is stored with the project and the included exchanges can be modified in each 'Program' file

# Project structure

- The project is divided into two parts
    - Console application
    - Web API application
- Service layer
    - StrategyService is responsible for finding the best way to fulfill an order
    - ExchangeService is for simulating the access to exchange data
- Model layer
    - OrderBook is the model for the order book data
    - Transaction is the model for the transaction data which is the 'strategy' to fulfill an order
- Tests
    - Unit tests for the service layer
        - Contains tests for a few different scenarios

# Assumptions

Since I'm not familiar with the crypto exchange domain, I made a few assumptions to simplify the implementation.

- I assume that an order can be partially fulfilled, so even I want to buy 1 btc I might only get 0.5 btc.
- I assume that the 'constraints' mean that I can only use the balance of the exchange to fulfill the order.
    - This check is used for both selling and buying
- My wording for things might be different from the actual domain terms, I tried to use the terms I'm familiar with.

# CryptoTransactionStrategy

This class is responsible for creating a potential transaction to fulfill an order request.  
It takes a Order and the whole Exchange as a list, but also supports single Exchanges.

# Improvements

There are a lot of improvement and Refactoring that could be done with this project.  
I've strived for a balance between time and quality.  
I could have used the Enum better.  
Better error handling, for now there is some validation on the data from the client, but generally errors are not handled. I have not checked for negative values or corrupted data etc.