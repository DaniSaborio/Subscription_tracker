using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoApi.Data;
using TodoApi.Models;

namespace TodoApi.Controllers;

[ApiController]
[Authorize]
[Route("subscriptions")]
public class SubscriptionsController : ControllerBase
{
    private static readonly HashSet<string> AllowedCycles = new(StringComparer.OrdinalIgnoreCase)
    {
        "weekly",
        "biweekly",
        "monthly",
        "quarterly",
        "yearly"
    };

    private readonly ISubscriptionRepository _repository;

    public SubscriptionsController(ISubscriptionRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<SubscriptionItem>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] string? billingCycle,
        [FromQuery] int? upcomingDays)
    {
        if (!string.IsNullOrWhiteSpace(billingCycle) && !AllowedCycles.Contains(billingCycle))
        {
            return BadRequest(new { message = "billingCycle no válido." });
        }

        var userId = GetUserId();
        var items = await _repository.GetAllAsync(userId, search, category, billingCycle, upcomingDays);
        return Ok(items);
    }

    [HttpGet("upcoming")]
    public async Task<ActionResult<IEnumerable<SubscriptionItem>>> GetUpcoming([FromQuery] int days = 30)
    {
        var userId = GetUserId();
        var items = await _repository.GetUpcomingAsync(userId, days);
        return Ok(items);
    }

    [HttpGet("summary")]
    public async Task<ActionResult<SubscriptionSummaryResponse>> GetSummary()
    {
        var userId = GetUserId();
        var items = (await _repository.GetAllAsync(userId, null, null, null, null)).ToList();

        var byCycle = items
            .GroupBy(i => i.BillingCycle.ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));

        var monthlyTotal = items.Sum(item => ToMonthlyEquivalent(item.Amount, item.BillingCycle));
        var summary = new SubscriptionSummaryResponse
        {
            MonthlyEquivalentTotal = decimal.Round(monthlyTotal, 2),
            YearlyEquivalentTotal = decimal.Round(monthlyTotal * 12m, 2),
            TotalsByBillingCycle = byCycle,
            UpcomingIn30Days = items.Count(i => i.NextBillingDate.Date <= DateTime.UtcNow.Date.AddDays(30))
        };

        return Ok(summary);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SubscriptionItem>> GetById(Guid id)
    {
        var userId = GetUserId();
        var item = await _repository.GetByIdAsync(userId, id);
        if (item is null)
        {
            return NotFound(new { message = "Suscripción no encontrada." });
        }

        return Ok(item);
    }

    [HttpPost]
    public async Task<ActionResult<SubscriptionItem>> Create([FromBody] CreateSubscriptionRequest request)
    {
        var validationError = ValidateRequest(request.Name, request.Category, request.BillingCycle, request.Amount, request.Currency);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        var userId = GetUserId();
        var created = await _repository.CreateAsync(userId, request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SubscriptionItem>> Update(Guid id, [FromBody] UpdateSubscriptionRequest request)
    {
        var validationError = ValidateRequest(request.Name, request.Category, request.BillingCycle, request.Amount, request.Currency);
        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        var userId = GetUserId();
        var updated = await _repository.UpdateAsync(userId, id, request);
        if (updated is null)
        {
            return NotFound(new { message = "Suscripción no encontrada." });
        }

        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var userId = GetUserId();
        var deleted = await _repository.DeleteAsync(userId, id);
        if (!deleted)
        {
            return NotFound(new { message = "Suscripción no encontrada." });
        }

        return NoContent();
    }

    private Guid GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(ClaimTypes.Name);
        if (!Guid.TryParse(value, out var userId))
        {
            throw new UnauthorizedAccessException("Token inválido.");
        }

        return userId;
    }

    private static string? ValidateRequest(string name, string category, string billingCycle, decimal amount, string currency)
    {
        if (string.IsNullOrWhiteSpace(name)) return "El nombre es obligatorio.";
        if (string.IsNullOrWhiteSpace(category)) return "La categoría es obligatoria.";
        if (string.IsNullOrWhiteSpace(currency)) return "La moneda es obligatoria.";
        if (amount <= 0) return "El monto debe ser mayor a cero.";
        if (!AllowedCycles.Contains(billingCycle)) return "billingCycle no válido.";
        return null;
    }

    private static decimal ToMonthlyEquivalent(decimal amount, string cycle)
    {
        return cycle.ToLowerInvariant() switch
        {
            "weekly" => amount * 52m / 12m,
            "biweekly" => amount * 26m / 12m,
            "monthly" => amount,
            "quarterly" => amount / 3m,
            "yearly" => amount / 12m,
            _ => amount
        };
    }
}