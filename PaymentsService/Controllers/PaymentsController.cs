using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaymentsService.Data;
using PaymentsService.Models;

namespace PaymentsService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountsController : ControllerBase
    {
        private readonly PaymentDbContext _db;
        public AccountsController(PaymentDbContext db) => _db = db;

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAccountDto dto)
        {
            if (await _db.Accounts.AnyAsync(a => a.UserId == dto.UserId))
                return Conflict("Account already exists");

            _db.Accounts.Add(new Account { UserId = dto.UserId, Balance = 0 });
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { userId = dto.UserId }, null);
        }

        [HttpPost("{userId:int}/deposit")]
        public async Task<IActionResult> Deposit(int userId, [FromBody] DepositDto dto)
        {
            var acc = await _db.Accounts.FindAsync(userId);
            if (acc == null) return NotFound();
            acc.Balance += dto.Amount;
            await _db.SaveChangesAsync();
            return Ok(acc);
        }

        [HttpGet("{userId:int}")]
        public async Task<IActionResult> Get(int userId)
        {
            var acc = await _db.Accounts.FindAsync(userId);
            return acc == null ? NotFound() : Ok(acc);
        }
    }

    public record CreateAccountDto(int UserId);
    public record DepositDto(decimal Amount);
}