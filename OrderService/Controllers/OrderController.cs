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

        public OrderController(OrderDbContext db)
        {
            _db = db;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateOrderDto dto)
        {
            using var tx = await _db.Database.BeginTransactionAsync();

            var order = new Order
            {
                Id = Guid.NewGuid(),
                UserId = dto.UserId,
                Amount = dto.Amount,
                Description = dto.Description,
                Status = OrderStatus.NEW,
                CreatedAt = DateTime.UtcNow
            };
            _db.Orders.Add(order);
            
            var evt = new
            {
                OrderId = order.Id,
                UserId = order.UserId,
                Amount = order.Amount
            };
            _db.Outbox.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                EventType = "OrderCreated",
                Payload = JsonSerializer.Serialize(evt),
                Published = false,
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