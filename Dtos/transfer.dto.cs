namespace Digital_Wallet_System.Dtos
{
    public class TransferRequest
    {
        public int RecipientUserId { get; set; }
        public decimal Amount { get; set; }
    }
}