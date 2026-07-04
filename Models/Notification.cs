namespace SmartSpice.Models;

/// <summary>
/// A system alert surfaced to the user (low stock, expiry, quality, shipment).
/// </summary>
public class Notification
{
    public int Id { get; set; }
    public NotificationType Type { get; set; }
    public NotificationSeverity Severity { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public bool IsRead { get; set; }
}

