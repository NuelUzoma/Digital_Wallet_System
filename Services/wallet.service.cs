using Digital_Wallet_System.Data;
using Digital_Wallet_System.Models;

namespace Digital_Wallet_System.Services
{
    public class WalletService
    {
        private readonly ApplicationDbContext _walletContext;

        public WalletService(ApplicationDbContext walletContext)
        {
            _walletContext = walletContext;
        }

        public async Task<Wallet> CreateWalletAsync(User user)
        {
            var wallet = new Wallet
            {
                Balance = 0,
                UserId = user.Id,
                User = user
            };

            _walletContext.Wallets.Add(wallet);
            await _walletContext.SaveChangesAsync();

            return wallet;
        }
    }
}