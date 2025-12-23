using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaymentService.Domain;

namespace PaymentService.Controllers;

public record CreateAccountDto(Guid UserId);
public record TopUpDto(Guid UserId, decimal Amount);

[ApiController]
[Route("api/accounts")]
public class AccountController : ControllerBase
{
    private readonly PaymentDbContext _db;
    
    public AccountController(PaymentDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAccountDto dto)
    {
        if (await _db.Accounts.AnyAsync(a => a.UserId == dto.UserId))
            return BadRequest("Account already exists");

        var account = new Account
        {
            UserId = dto.UserId,
            Balance = 0
        };

        _db.Accounts.Add(account);
        await _db.SaveChangesAsync();

        return Ok();
    }

    [HttpPost("topup")]
    public async Task<IActionResult> TopUp([FromBody] TopUpDto dto)
    {
        if (dto.Amount <= 0)
            return BadRequest("Amount must be positive");
        var account = await _db.Accounts.FirstOrDefaultAsync(a => a.UserId == dto.UserId);
        if (account == null)
            return NotFound("Account not found");

        account.Balance += dto.Amount;
        await _db.SaveChangesAsync();

        return Ok(account.Balance);
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetBalance(Guid userId)
    {
        var account = await _db.Accounts.FirstOrDefaultAsync(a => a.UserId == userId);
        if (account == null)
            return NotFound();

        return Ok(new { account.Balance });
    }
}