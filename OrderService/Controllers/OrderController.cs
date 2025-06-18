using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Models;
using System.Text.Json;

namespace OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
    private readonly OrderDbContext _db;
    private readonly IHttpClientFactory _http;

        public OrderController(OrderDbContext db, IHttpClientFactory http)
        {
            _db = db;
            _http = http;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOrderDto dto)
        {
            // 0) Проверяем, что аккаунт существует и баланс >= сумме заказа
            var client = _http.CreateClient();
            client.BaseAddress = new Uri("http://payments-service"); // из docker-compose
            var resp = await client.GetAsync($"/api/accounts/{dto.UserId}");
            if (!resp.IsSuccessStatusCode)
                return BadRequest($"Account {dto.UserId} not found");

            var account = await resp.Content.ReadFromJsonAsync<AccountDto>();
            if (account!.Balance < dto.Amount)
                return BadRequest("Insufficient funds");

            // 1) Создаём заказ и кладём событие в Outbox
            using var tx = await _db.Database.BeginTransactionAsync();

            var order = new Order {
                Id          = Guid.NewGuid(),
                UserId      = dto.UserId,
                Amount      = dto.Amount,
                Description = dto.Description,
                Status      = OrderStatus.NEW,
                CreatedAt   = DateTime.UtcNow
            };
            _db.Orders.Add(order);

            var evt = new {
                OrderId = order.Id,
                UserId  = order.UserId,
                Amount  = order.Amount
            };
            _db.Outbox.Add(new OutboxMessage {
                Id         = Guid.NewGuid(),
                EventType  = "OrderCreated",
                Payload    = JsonSerializer.Serialize(evt),
                Published  = false,
                OccurredAt = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);

        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Order>>> GetAll()
        {
            var all = await _db.Orders
                .OrderBy(o => o.CreatedAt)
                .ToListAsync();
            return Ok(all);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<Order>> GetById(Guid id)
        {
            var order = await _db.Orders.FindAsync(id);
            if (order == null) return NotFound();
            return Ok(order);
        }
}

public record CreateOrderDto(int UserId, decimal Amount, string Description);
public record AccountDto(int UserId, decimal Balance);