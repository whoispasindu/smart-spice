using SmartSpice.Data;
using SmartSpice.Models;

namespace SmartSpice.Services;

public interface INotificationService
{
    IReadOnlyList<Notification> GetAll();
    int UnreadCount();
    void MarkAllRead();
    void RefreshLowStockAlerts();
}

public class NotificationService : INotificationService
{
    public IReadOnlyList<Notification> GetAll()
    {
        using var db = new SmartSpiceContext();
        return db.Notifications.OrderByDescending(n => n.CreatedAt).Take(100).ToList();
    }

    public int UnreadCount()
    {
        using var db = new SmartSpiceContext();
        return db.Notifications.Count(n => !n.IsRead);
    }

    public void MarkAllRead()
    {
        using var db = new SmartSpiceContext();
        foreach (var n in db.Notifications.Where(n => !n.IsRead))
            n.IsRead = true;
        db.SaveChanges();
    }

    public void RefreshLowStockAlerts()
    {
        using var db = new SmartSpiceContext();
        var low = db.InventoryItems.Where(i => i.QuantityKg <= i.ReorderLevelKg).ToList();

        foreach (var item in low)
        {
            string title = $"Low stock: {item.SpiceName}";
            // Avoid duplicating an existing unread alert for the same item.
            bool exists = db.Notifications.Any(n =>
                n.Type == NotificationType.LowStock && !n.IsRead && n.Title == title);
            if (exists) continue;

            db.Notifications.Add(new Notification
            {
                Type = NotificationType.LowStock,
                Severity = item.QuantityKg <= item.ReorderLevelKg / 2
                    ? NotificationSeverity.Critical
                    : NotificationSeverity.Warning,
                Title = title,
                Message = $"{item.SpiceName} is at {item.QuantityKg:N0} kg (reorder at {item.ReorderLevelKg:N0} kg)."
            });
        }
        db.SaveChanges();
    }
}
