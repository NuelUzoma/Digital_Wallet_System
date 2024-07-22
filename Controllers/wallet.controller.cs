using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Digital_Wallet_System.Data;
using Digital_Wallet_System.Dtos;
using Digital_Wallet_System.Models;
using Digital_Wallet_System.Services;
using static Digital_Wallet_System.Services.WalletService;

namespace Digital_Wallet_System.Controllers
{
    [ApiController]
    [Route("api/wallet")]
    public class WalletController: ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly WalletService _walletService;

        public WalletController(ApplicationDbContext context, WalletService walletService)
        {
            _context = context;
            _walletService = walletService;
        }

        // Re-usable function to retrieve the userId from JWT payload
        private ActionResult<int> GetUserId()
        {
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

        // Retrieve the wallet details of the logged-in user
        [HttpGet]
        public async Task<ActionResult<Wallet>> GetWallet()
        {
            // Retrieve userId
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
        public async Task<IActionResult> Deposit([FromBody] DepositRequest request)
        {
            // Retrieve userId
            var userIdResult = GetUserId();
            
            if (userIdResult.Result is UnauthorizedObjectResult || userIdResult.Result is BadRequestObjectResult)
            {
                return userIdResult.Result; // Return the error result
            }

            // Integer value for the userId
            int userId = userIdResult.Value;

            var depositResult = await _walletService.DepositFundsAsync(userId, request.Amount);
            
            return depositResult switch
            {
                DepositResult.Success => Ok(new { message = "Deposit Successful" }),
                DepositResult.UserNotFound => NotFound("User not found"),
                DepositResult.InvalidAmount => BadRequest("Invalid deposit amount"),
                DepositResult.UnknownError => StatusCode(500, "An unexpected error occurred"),
                _ => StatusCode(500, "An unexpected error occurred"),
            };
        }

        // Wallet to wallet transfer between users
        [HttpPost("transfer")]
        public async Task<IActionResult> Transfer([FromBody] TransferRequest request)
        {
            // Retrieve userId
            var userIdResult = GetUserId();
            
            if (userIdResult.Result is UnauthorizedObjectResult || userIdResult.Result is BadRequestObjectResult)
            {
                return userIdResult.Result; // Return the error result
            }

            // Integer value for the userId
            int senderId = userIdResult.Value;

            
            var transferResult = await _walletService.TransferFundsAsync(senderId, request.RecipientUserId, request.Amount);
            
            return transferResult switch
            {
                TransferResult.Success => Ok(new { message = "Transfer Successful" }),
                TransferResult.SenderNotFound => NotFound("Sender not found"),
                TransferResult.RecipientNotFound => NotFound("Recipient not found"),
                TransferResult.SameWalletTransfer => BadRequest("Transfer to the same wallet is prohibited"),
                TransferResult.InsufficientFunds => BadRequest("Insufficient funds"),
                TransferResult.InvalidAmount => BadRequest("Invalid transfer amount"),
                TransferResult.UnknownError => StatusCode(500, "An unexpected error occurred"),
                _ => StatusCode(500, "An unexpected error occurred"),
            };
        }

        // Retrieve debit wallet transfer transactions
        [HttpGet("transactions/debit")]
        public async Task<IActionResult> GetDebitTransactions()
        {
            // Retrieve userId
            var userIdResult = GetUserId();
            
            if (userIdResult.Result is UnauthorizedObjectResult || userIdResult.Result is BadRequestObjectResult)
            {
                return userIdResult.Result; // Return the error result
            }

            // Integer value for the userId
            int userId = userIdResult.Value;

            var transactions = await _walletService.GetDebitTransactionsAsync(userId);
            
            return Ok(transactions);
        }

        // Retrieve credit wallet transfer transactions
        [HttpGet("transactions/credit")]
        public async Task<IActionResult> GetCreditTransactions()
        {
            // Retrieve userId
            var userIdResult = GetUserId();
            
            if (userIdResult.Result is UnauthorizedObjectResult || userIdResult.Result is BadRequestObjectResult)
            {
                return userIdResult.Result; // Return the error result
            }

            // Integer value for the userId
            int userId = userIdResult.Value;

            var transactions = await _walletService.GetCreditTransactionsAsync(userId);
            
            return Ok(transactions);
        }
    }
}