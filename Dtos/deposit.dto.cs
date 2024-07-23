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
}