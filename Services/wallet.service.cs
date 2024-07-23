using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Digital_Wallet_System.Data;
using Digital_Wallet_System.Models;

namespace Digital_Wallet_System.Services
{
    public class WalletService
    {
        private readonly ApplicationDbContext _context;
        private readonly RedisService _redisService;
        private readonly PaystackService _paystackService;
        private readonly ReferenceProtector _referenceProtector;

        public WalletService(ApplicationDbContext context, RedisService redisService, PaystackService paystackService, ReferenceProtector referenceProtector)
        {
            _context = context;
            _redisService = redisService;
            _paystackService = paystackService;
            _referenceProtector = referenceProtector;
        }

        public async Task<Wallet> CreateWalletAsync(User user)
        {
            var wallet = new Wallet
            {
                Balance = 0,
                UserId = user.Id,
                User = user
            };

            _context.Wallets.Add(wallet);
            await _context.SaveChangesAsync();

            return wallet;
        }

        // Initiate paystack deposit
        public async Task<(string, string)> InitiateDepositAsync(int userId, decimal amount)
        {
            var user = await _context.Users.Include(u => u.Wallet).FirstOrDefaultAsync(u => u.Id == userId) ?? throw new ArgumentException("User not found");
            var response = await _paystackService.InitializeTransaction(amount, user.Email);

            if (amount <= 0 || amount % 1 != 0) // Validate amount to be deposited
                throw new InvalidOperationException("Invalid Deposit Amount");

            var encryptedReference = _referenceProtector.Protect(response.Data.Reference);

            // Store pending transaction
            var transaction = new DepositTransaction
            {
                UserId = userId,
                Amount = amount,
                Timestamp = DateTime.UtcNow.AddHours(1),
                TransactionType = "Deposit",
                Status = "Pending",
                Reference = encryptedReference
            };

            _context.DepositTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            // Return the authorization url and original (unencrypted) reference
            return (response.Data.AuthorizationUrl, response.Data.Reference);
        }

        // Verify paystack deposit logic
        public async Task<DepositResult> VerifyandCompleteDepositAsync(string reference, int userId)
        {
            // Decrypt all references in the database to find a match
            var transactions = await _context.DepositTransactions
                .Where(t => t.UserId == userId)
                .ToListAsync();
            
            var transaction = transactions.FirstOrDefault(t => t.Reference != null && _referenceProtector.Unprotect(t.Reference) == reference);
            
            if (transaction == null)
                return DepositResult.TransactionNotFound;

            // Verify deposit transaction
            var verificationResponse = await _paystackService.VerifyTransaction(reference);

            if (verificationResponse.Data.Status == "success")
            {
                var user = await _context.Users.Include(u => u.Wallet).FirstOrDefaultAsync(u => u.Id == transaction.UserId);

                if (user == null)
                    return DepositResult.UserNotFound;

                // Perform the deposit
                user.Wallet.Balance += transaction.Amount;
                transaction.Status = "Completed";

                // Save changes
                await _context.SaveChangesAsync();

                return DepositResult.Success;
            }
            else
            {
                transaction.Status = "Failed";
                await _context.SaveChangesAsync();
                return DepositResult.PaymentFailed;
            }
        }

        // Wallet transfer logic
        public async Task<TransferResult> TransferFundsAsync(int senderId, int recipientId, decimal amount, string idempotencyKey)
        {
            var redis = _redisService.GetDatabase();

            // Check if request has been processed before with idempotency key
            RedisValue existingValue = await redis.StringGetAsync(idempotencyKey);
            if (existingValue.HasValue && existingValue == "processed")
            {
                return TransferResult.AlreadyProcessed;
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var sender = await _context.Users.Include(u => u.Wallet).FirstOrDefaultAsync(u => u.Id == senderId);
                var recipient = await _context.Users.Include(u => u.Wallet).FirstOrDefaultAsync(u => u.Id == recipientId);

                if (sender == null)
                    return TransferResult.SenderNotFound;
                if (recipient == null)
                    return TransferResult.RecipientNotFound;
                if (sender.Id == recipient.Id)
                    return TransferResult.SameWalletTransfer;
                if (sender.Wallet.Balance < amount)
                    return TransferResult.InsufficientFunds;
                if (amount <= 0 || amount % 1 != 0)
                    return TransferResult.InvalidAmount;

                // Perform the transfer and update individual wallets
                sender.Wallet.Balance -= amount;
                recipient.Wallet.Balance += amount;

                // Create sender's transaction
                var senderTransaction = new TransferTransaction
                {
                    SenderId = senderId,
                    RecipientId = recipientId,
                    Amount = amount,
                    Timestamp = DateTime.UtcNow.AddHours(1),
                    TransactionType = "Debit"
                };

                // Create receiver's transaction
                var recipientTransaction = new TransferTransaction
                {
                    SenderId = senderId,
                    RecipientId = recipientId,
                    Amount = amount,
                    Timestamp = DateTime.UtcNow.AddHours(1),
                    TransactionType = "Credit"
                };

                _context.TransferTransactions.Add(senderTransaction);
                _context.TransferTransactions.Add(recipientTransaction);

                // If successful, set the idempotency key in Redis with a 30-second expiry
                await redis.StringSetAsync(idempotencyKey, "processed", TimeSpan.FromSeconds(30));
                
                await _context.SaveChangesAsync(); // Save changes
                await transaction.CommitAsync();

                return TransferResult.Success;
            }
            catch
            {
                await transaction.RollbackAsync();
                return TransferResult.UnknownError;
            }
        }

        // Get user's deposit transactions
        public async Task<IEnumerable<DepositTransaction>> GetDepositTransactionsAsync(int userId)
        {
            return await _context.DepositTransactions
                .Where(d => d.UserId == userId)
                .OrderByDescending(d => d.Timestamp)
                .ToListAsync();
        }

        // Get debit transactions by userId
        public async Task<IEnumerable<TransferTransaction>> GetDebitTransactionsAsync(int userId)
        {
            return await _context.TransferTransactions
                .Where(t => t.SenderId == userId & t.TransactionType == "Debit")
                .OrderByDescending(t => t.Timestamp)
                .ToListAsync();
        }

        // Get credit transactions by userId
        public async Task<IEnumerable<TransferTransaction>> GetCreditTransactionsAsync(int userId)
        {
            return await _context.TransferTransactions
                .Where(t => t.RecipientId == userId & t.TransactionType == "Credit")
                .OrderByDescending(t => t.Timestamp)
                .ToListAsync();
        }

        // Standard responses for deposits
        public enum DepositResult
        {
            Success,
            UserNotFound,
            TransactionNotFound,
            InvalidAmount,
            PaymentFailed,
            UnknownError
        }

        // Standard responses for transfers
        public enum TransferResult
        {
            Success,
            SenderNotFound,
            RecipientNotFound,
            SameWalletTransfer,
            InsufficientFunds,
            InvalidAmount,
            AlreadyProcessed,
            UnknownError
        }
    }
}