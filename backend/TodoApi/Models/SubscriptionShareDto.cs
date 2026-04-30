namespace TodoApi.Models;

public class SubscriptionShareDto
{
    public Guid SharedWithUserId { get; set; }
    public string SharedWithEmail { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
