using Microsoft.EntityFrameworkCore;
using Digital_Wallet_System.Data;
using Digital_Wallet_System.Models;

namespace Digital_Wallet_System.Services
{
    public class WalletService
    {
        private readonly ApplicationDbContext _context;

        public WalletService(ApplicationDbContext context)
        {
            _context = context;
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

        // Deposit funds logic
        public async Task<DepositResult> DepositFundsAsync(int userId, decimal amount)
        {
            var user = await _context.Users.Include(u => u.Wallet).FirstOrDefaultAsync(u => u.Id == userId);
            
            if (user == null)
                return DepositResult.UserNotFound;
            if (amount <= 0 || amount % 1 != 0) // Validate amount to be transfered
                return DepositResult.InvalidAmount;

            // Perform the deposit
            user.Wallet.Balance += amount;

            // Save changes
            await _context.SaveChangesAsync();
            
            return DepositResult.Success;
        }

        // Wallet transfer logic
        public async Task<TransferResult> TransferFundsAsync(int senderId, int recipientId, decimal amount)
        {
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
                var senderTransaction = new Transaction
                {
                    SenderId = senderId,
                    RecipientId = recipientId,
                    Amount = amount,
                    Timestamp = DateTime.UtcNow,
                    TransactionType = "Debit"
                };

                // Create receiver's transaction
                var recipientTransaction = new Transaction
                {
                    SenderId = senderId,
                    RecipientId = recipientId,
                    Amount = amount,
                    Timestamp = DateTime.UtcNow,
                    TransactionType = "Credit"
                };

                _context.Transactions.Add(senderTransaction);
                _context.Transactions.Add(recipientTransaction);

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

        // Get debit transactions by userId
        public async Task<IEnumerable<Transaction>> GetDebitTransactionsAsync(int userId)
        {
            return await _context.Transactions
                .Where(t => t.SenderId == userId & t.TransactionType == "Debit")
                .OrderByDescending(t => t.Timestamp)
                .ToListAsync();
        }

        // Get credit transactions by userId
        public async Task<IEnumerable<Transaction>> GetCreditTransactionsAsync(int userId)
        {
            return await _context.Transactions
                .Where(t => t.RecipientId == userId & t.TransactionType == "Credit")
                .OrderByDescending(t => t.Timestamp)
                .ToListAsync();
        }

        // Standard responses for deposits
        public enum DepositResult
        {
            Success,
            UserNotFound,
            InvalidAmount,
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
            UnknownError
        }
    }
}