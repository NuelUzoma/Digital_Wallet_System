using PayStack.Net;

public class PaystackService
{
    private readonly PayStackApi _paystackApi;

    public PaystackService(IConfiguration configuration)
    {
        var secretKey = configuration["Paystack:SecretKey"] ?? throw new InvalidOperationException("Paystack configuration is missing.");
        _paystackApi = new PayStackApi(secretKey);
    }

    public Task<TransactionInitializeResponse> InitializeTransaction(decimal amount, string email, string currency = "NGN")
    {
        var request = new TransactionInitializeRequest
        {
            AmountInKobo = (int)(amount * 100), // Convert to kobo
            Email = email,
            Currency = currency
        };

        return Task.FromResult(_paystackApi.Transactions.Initialize(request));
    }

    public Task<TransactionVerifyResponse> VerifyTransaction(string reference)
    {
        return Task.FromResult( _paystackApi.Transactions.Verify(reference));
    }
}