using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Subscription_tracker.API.Data;
using Subscription_tracker.API.DTOs;
using Subscription_tracker.API.Models;

namespace Subscription_tracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubscriptionsController(AppDbContext context) : ControllerBase
{
    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return int.Parse(userIdClaim?.Value ?? "0");
    }

    [HttpGet]
    public async Task<ActionResult<List<SubscriptionDto>>> GetSubscriptions()
    {
        var userId = GetUserId();
        var subscriptions = await context.Subscriptions
            .Where(s => s.UserId == userId)
            .OrderBy(s => s.NextBillingDate)
            .ToListAsync();

        var dtos = subscriptions.Select(s => new SubscriptionDto(
            s.Id, s.ServiceName, s.Category, s.Amount, s.BillingCycle,
            s.NextBillingDate, s.IsActive, s.PaymentMethod, s.Notes
        )).ToList();

        return Ok(dtos);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SubscriptionDto>> GetSubscription(int id)
    {
        var userId = GetUserId();
        var subscription = await context.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

        if (subscription == null)
            return NotFound();

        var dto = new SubscriptionDto(
            subscription.Id, subscription.ServiceName, subscription.Category,
            subscription.Amount, subscription.BillingCycle, subscription.NextBillingDate,
            subscription.IsActive, subscription.PaymentMethod, subscription.Notes
        );

        return Ok(dto);
    }

    [HttpPost]
    public async Task<ActionResult<SubscriptionDto>> CreateSubscription([FromBody] SubscriptionDto dto)
    {
        var userId = GetUserId();

        var subscription = new Subscription
        {
            UserId = userId,
            ServiceName = dto.ServiceName,
            Category = dto.Category,
            Amount = dto.Amount,
            BillingCycle = dto.BillingCycle,
            NextBillingDate = dto.NextBillingDate,
            IsActive = dto.IsActive,
            PaymentMethod = dto.PaymentMethod,
            Notes = dto.Notes,
            UpdatedAt = DateTime.UtcNow
        };

        context.Subscriptions.Add(subscription);
        await context.SaveChangesAsync();

        var responseDto = new SubscriptionDto(
            subscription.Id, subscription.ServiceName, subscription.Category,
            subscription.Amount, subscription.BillingCycle, subscription.NextBillingDate,
            subscription.IsActive, subscription.PaymentMethod, subscription.Notes
        );

        return CreatedAtAction(nameof(GetSubscription), new { id = subscription.Id }, responseDto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateSubscription(int id, [FromBody] SubscriptionDto dto)
    {
        var userId = GetUserId();
        var subscription = await context.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

        if (subscription == null)
            return NotFound();

        subscription.ServiceName = dto.ServiceName;
        subscription.Category = dto.Category;
        subscription.Amount = dto.Amount;
        subscription.BillingCycle = dto.BillingCycle;
        subscription.NextBillingDate = dto.NextBillingDate;
        subscription.IsActive = dto.IsActive;
        subscription.PaymentMethod = dto.PaymentMethod;
        subscription.Notes = dto.Notes;
        subscription.UpdatedAt = DateTime.UtcNow;

        context.Subscriptions.Update(subscription);
        await context.SaveChangesAsync();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSubscription(int id)
    {
        var userId = GetUserId();
        var subscription = await context.Subscriptions
            .FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);

        if (subscription == null)
            return NotFound();

        context.Subscriptions.Remove(subscription);
        await context.SaveChangesAsync();

        return NoContent();
    }

    [HttpPost("sync")]
    public async Task<IActionResult> SyncChanges([FromBody] List<SyncChangeDto> changes)
    {
        var userId = GetUserId();

        foreach (var change in changes)
        {
            if (change.EntityType != "subscription") continue;

            if (change.OperationType == "create" && change.Payload is System.Text.Json.JsonElement createPayload)
            {
                var dto = System.Text.Json.JsonSerializer.Deserialize<SubscriptionDto>(createPayload.GetRawText());
                if (dto != null)
                {
                    var subscription = new Subscription
                    {
                        UserId = userId,
                        ServiceName = dto.ServiceName,
                        Category = dto.Category,
                        Amount = dto.Amount,
                        BillingCycle = dto.BillingCycle,
                        NextBillingDate = dto.NextBillingDate,
                        IsActive = dto.IsActive,
                        PaymentMethod = dto.PaymentMethod,
                        Notes = dto.Notes,
                        UpdatedAt = DateTime.UtcNow
                    };
                    context.Subscriptions.Add(subscription);
                }
            }
            else if (change.OperationType == "update" && change.Payload is System.Text.Json.JsonElement updatePayload)
            {
                var dto = System.Text.Json.JsonSerializer.Deserialize<SubscriptionDto>(updatePayload.GetRawText());
                if (dto?.Id != null)
                {
                    var subscription = await context.Subscriptions
                        .FirstOrDefaultAsync(s => s.Id == dto.Id && s.UserId == userId);
                    if (subscription != null)
                    {
                        subscription.ServiceName = dto.ServiceName;
                        subscription.Category = dto.Category;
                        subscription.Amount = dto.Amount;
                        subscription.BillingCycle = dto.BillingCycle;
                        subscription.NextBillingDate = dto.NextBillingDate;
                        subscription.IsActive = dto.IsActive;
                        subscription.PaymentMethod = dto.PaymentMethod;
                        subscription.Notes = dto.Notes;
                        subscription.UpdatedAt = DateTime.UtcNow;
                        context.Subscriptions.Update(subscription);
                    }
                }
            }
            else if (change.OperationType == "delete" && change.Payload is System.Text.Json.JsonElement deletePayload)
            {
                var idObj = deletePayload.GetProperty("id");
                if (int.TryParse(idObj.GetString(), out var deleteId))
                {
                    var subscription = await context.Subscriptions
                        .FirstOrDefaultAsync(s => s.Id == deleteId && s.UserId == userId);
                    if (subscription != null)
                        context.Subscriptions.Remove(subscription);
                }
            }
        }

        await context.SaveChangesAsync();
        return Ok(new { message = "Sync completed" });
    }
}
