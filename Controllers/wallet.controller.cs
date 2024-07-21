using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Digital_Wallet_System.Data;
using Digital_Wallet_System.Dtos;
using Digital_Wallet_System.Models;

namespace Digital_Wallet_System.Controllers
{
    [ApiController]
    [Route("api/wallet")]
    public class WalletController: ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public WalletController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Re-usable function to retrieve the userId from JWT payload
        private ActionResult<int> GetUserId()
        {
            // Get the logged-in user's ID from the JWT token
            // var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var claimsIdentity = User.Identity as ClaimsIdentity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.Name)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                // Return unauthorized if UserId is not found in the token
                return Unauthorized("User ID is not found in the token.");
            }

            if (!int.TryParse(userId, out int userIdInt))
            {
                return BadRequest("Invalid User ID in token.");
            }

            return userIdInt;
        }

        // Re-usuable function to validate the deposit/transfer amount
        private void ValidateDepositAmount(decimal amount)
        {
            if (amount <= 0 || amount % 1 != 0)
            {
                throw new ArgumentException("Deposit amount must be a positive integer greater than 0.");
            }
        }

        // Retrieve the wallet details by the logged-in user
        [HttpGet]
        public async Task<ActionResult<Wallet>> GetWallet()
        {
            // Retrieve userId from the <actionresult> function
            var userIdResult = GetUserId();
            if (userIdResult.Result is UnauthorizedObjectResult || userIdResult.Result is BadRequestObjectResult)
            {
                return userIdResult.Result; // Return the error result
            }

            // Integer value for the userId
            int userId = userIdResult.Value;

            // Retrieve wallet details for the userId
            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
            if (wallet == null)
            {
                return NotFound("Wallet not found for the logged-in user.");
            }

            return Ok(wallet);
        }

        // Deposit funds into one's wallet
        [HttpPost("deposit")]
        public async Task<ActionResult<Wallet>> Deposit([FromBody] DepositRequest request)
        {
            // Retrieve userId from the <actionresult> function
            var userIdResult = GetUserId();
            if (userIdResult.Result is UnauthorizedObjectResult || userIdResult.Result is BadRequestObjectResult)
            {
                return userIdResult.Result; // Return the error result
            }

            // Integer value for the userId
            int userId = userIdResult.Value;

            // Retrieve the user wallet
            var user = await _context.Users.Include(u => u.Wallet).FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return NotFound("User not found");
            }

            // Validate amount to be deposited
            try
            {
                ValidateDepositAmount(request.Amount);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message); // Return error upon invalidation
            }

            // Perform the deposit
            user.Wallet.Balance += request.Amount;
            await _context.SaveChangesAsync();

            // Return the wallet balance
            return Ok(new { message = "Deposit Successful" });
        }

        // Wallet to wallet transfer between users
        [HttpPost("transfer")]
        public async Task<ActionResult<Wallet>> Transfer([FromBody] TransferRequest request)
        {
            // Retrieve userId from the <actionresult> function
            var userIdResult = GetUserId();
            if (userIdResult.Result is UnauthorizedObjectResult || userIdResult.Result is BadRequestObjectResult)
            {
                return userIdResult.Result; // Return the error result
            }

            // Integer value for the userId
            int userId = userIdResult.Value;

            // Sender wallet
            var sender = await _context.Users.Include(u => u.Wallet).FirstOrDefaultAsync(u => u.Id == userId);
            if (sender == null)
            {
                return NotFound("Sender not found");
            }

            // Reciever wallet
            var receiver = await _context.Users.Include(u => u.Wallet).FirstOrDefaultAsync(u => u.Id == request.RecipientUserId);
            if (receiver == null)
            {
                return NotFound("Recipient not found");
            }

            // PROHIBIT same wallet transfer i.e same user
            if (sender.Id == receiver.Id)
            {
                return BadRequest("Transfer to the same wallet is prohibited");
            }

            // Validate amount to be transfered
            try
            {
                ValidateDepositAmount(request.Amount);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message); // Return error upon invalidation
            }

            // Validate for insufficient funds in sender's wallet
            if (sender.Wallet.Balance < request.Amount)
            {
                return BadRequest("Insufficient funds");
            }

            // Perform the transfer and update individual wallets
            sender.Wallet.Balance -= request.Amount;
            receiver.Wallet.Balance += request.Amount;
            
            // Save changes
            await _context.SaveChangesAsync();

            return Ok(new { message = "Transfer Successful" });
        }

    }
}