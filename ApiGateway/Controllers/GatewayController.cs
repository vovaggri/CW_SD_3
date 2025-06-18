using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ApiGateway.Controllers;

[ApiController]
[Route("")]
public class GatewayController : ControllerBase
{
    private readonly ServiceUrls _urls;
    private readonly HttpClient _client;

    public GatewayController(IOptions<ServiceUrls> opts, HttpClient client)
    {
        _urls   = opts.Value;
        _client = client;
    }

    // === ORDERS ===
    [HttpPost("orders")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
    {
        var resp = await _client.PostAsJsonAsync($"{_urls.OrdersService}/api/order", dto);
        return StatusCode((int)resp.StatusCode, await resp.Content.ReadAsStringAsync());
    }

    [HttpGet("orders")]
    public async Task<IActionResult> GetAllOrders()
    {
        var resp = await _client.GetAsync($"{_urls.OrdersService}/api/order");
        if (!resp.IsSuccessStatusCode)
            return StatusCode((int)resp.StatusCode, await resp.Content.ReadAsStringAsync());

        var list = await resp.Content.ReadFromJsonAsync<IEnumerable<OrderSummaryDto>>();
        return Ok(list);
    }

    [HttpGet("orders/{id:guid}")]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        var resp = await _client.GetAsync($"{_urls.OrdersService}/api/order/{id}");
        if (resp.StatusCode == HttpStatusCode.NotFound)
            return NotFound();

        var dto = await resp.Content.ReadFromJsonAsync<OrderSummaryDto>();
        return Ok(dto);
    }

    // === ACCOUNTS ===
    [HttpPost("accounts")]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountDto dto)
    {
        var resp = await _client.PostAsJsonAsync($"{_urls.PaymentsService}/api/accounts", dto);
        return StatusCode((int)resp.StatusCode, await resp.Content.ReadAsStringAsync());
    }

    [HttpPost("accounts/{userId:int}/deposit")]
    public async Task<IActionResult> Deposit(int userId, [FromBody] DepositDto dto)
    {
        var resp = await _client.PostAsJsonAsync(
            $"{_urls.PaymentsService}/api/accounts/{userId}/deposit", dto);
        return StatusCode((int) resp.StatusCode, await resp.Content.ReadAsStringAsync());
    }

    [HttpGet("accounts/{userId:int}")]
    public async Task<IActionResult> GetBalance(int userId)
    {
        var resp = await _client.GetAsync($"{_urls.PaymentsService}/api/accounts/{userId}");
        if (resp.StatusCode == HttpStatusCode.NotFound) return NotFound();
        return Ok(await resp.Content.ReadFromJsonAsync<AccountDto>());
    }
}

// DTO-классы для GatewayController
public record CreateOrderDto(int UserId, decimal Amount, string Description);
public record OrderSummaryDto(Guid Id, int UserId, decimal Amount, string Description, OrderStatus Status, DateTime CreatedAt);

public record CreateAccountDto(int UserId);
public record DepositDto(decimal Amount);
public record AccountDto(int UserId, decimal Balance);