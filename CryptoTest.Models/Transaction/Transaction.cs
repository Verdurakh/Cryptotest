namespace CryptoTest.Models.Transaction;

public class Transaction
{
    public Guid FullfillmentId { get; set; }
    public decimal FullfillmentAmount { get; set; }
    public decimal FullfillmentPrice { get; set; }
    public List<TransactionOrder> TransactionOrders { get; set; } = new();
    public decimal UnfulfilledAmount { get; set; }
}