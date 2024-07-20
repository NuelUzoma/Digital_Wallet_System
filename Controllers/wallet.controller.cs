using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Digital_Wallet_System.Data;
using Digital_Wallet_System.Dtos;
using Digital_Wallet_System.Models;
using Digital_Wallet_System.Services;

namespace Digital_Wallet_System.Controllers
{
    [ApiController]
    [Route("api/wallet")]
    public class WalletController: ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<WalletController> _logger;

        public WalletController(ApplicationDbContext context, ILogger<WalletController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Re-usable function to retrieve the userId from JWT payload
        private ActionResult<int> GetUserId()
        {
            // Get the logged-in user's ID from the JWT token
            // var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var claimsIdentity = this.User.Identity as ClaimsIdentity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.Name)?.Value;

            _logger.LogInformation("User ID extracted from token: {userId}", userId);

            if (string.IsNullOrEmpty(userId))
            {
                // Return unauthorized if UserId is not found in the token
                _logger.LogWarning("User ID is not found in the token.");
                return Unauthorized("User ID is not found in the token.");
            }

            if (!int.TryParse(userId, out int userIdInt))
            {
                _logger.LogWarning("Invalid User ID in token: {userId}", userId);
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

        // Retrieve the logged in user details
        [HttpGet("user")]
        public async Task<ActionResult<User>> GetLoggedInUser()
        {
            // Retrieve userId from the <actionresult> function
            var userIdResult = GetUserId();
            if (userIdResult.Result is UnauthorizedObjectResult || userIdResult.Result is BadRequestObjectResult)
            {
                return userIdResult.Result; // Return the error result
            }

            // Integer value for the userId
            int userId = userIdResult.Value;

            var user = await _context.Users.Include(u => u.Wallet).FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
            {
                return NotFound("User not found");
            }

            return Ok(user);
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
            return Ok(new { user.Wallet.Balance }); // Not safe to return the wallet balance
        }

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

            return Ok(new { SenderBalance = sender.Wallet.Balance, RecipientBalance = receiver.Wallet.Balance });
        }

    }
}