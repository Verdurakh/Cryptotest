namespace CryptoTest.Models.Transaction;

public class TransactionOrder
{
    public decimal TransactionAmount { get; set; }
    public decimal TransactionPrice { get; set; }
    public Guid OrderId { get; set; }
    public decimal OrderRemainingAmount { get; set; }
    public decimal OrderOriginalAmount { get; set; }
    public decimal OrderPrice { get; set; }
    public string Exchange { get; set; }
}