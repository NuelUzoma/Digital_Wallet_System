namespace Digital_Wallet_System.Dtos
{
    public class DepositRequest
    {
        public decimal Amount { get; set; }
    }

    public class VerifyDepositRequest
    {
        public required string Reference { get; set; }
    }

    public class DepositResponse
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public DateTime Timestamp { get; set; }
        public string? Status { get; set; }
        public string? Reference { get; set; }
    }
}