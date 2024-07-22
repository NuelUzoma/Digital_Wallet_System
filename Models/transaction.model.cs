namespace Digital_Wallet_System.Models
{
    public class Transaction
    {
        public int Id { get; set; }
        public int SenderId { get; set; }
        public int RecipientId { get; set; }
        public decimal Amount { get; set; }
        public DateTime Timestamp { get; set; }
        public string? TransactionType { get; set; }

        // Navigation properties to User
        public User? Sender { get; set; }
        public User? Recipient { get; set; }
    }
}