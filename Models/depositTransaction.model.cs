namespace Digital_Wallet_System.Models
{
    public class DepositTransaction
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public DateTime Timestamp { get; set; }
        public string? Status { get; set; } // "Pending", "Completed", "Failed"
        public string? Reference { get; set; } // Paystack reference
        public string? ReferenceHash { get; set; }
        public string? TransactionType { get; set; }

        // Navigation property to User
        public User? User { get; set; }
    }
}