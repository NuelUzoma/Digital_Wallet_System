namespace Digital_Wallet_System.Models
{
    public class Wallet
    {
        public int Id { get; set; }
        public decimal Balance { get; set; }
        public int UserId { get; set; } // Foreign key to User table
        public User? User { get; set; } // Navigation property to User
    }
}