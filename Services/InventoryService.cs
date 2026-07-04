using Microsoft.EntityFrameworkCore;
using SmartSpice.Data;
using SmartSpice.Models;

namespace SmartSpice.Services;

public class InventoryService : IInventoryService
{
    private readonly IAuditService _audit;
    private readonly INotificationService _notifications;

    public InventoryService(IAuditService audit, INotificationService notifications)
    {
        _audit = audit;
        _notifications = notifications;
    }

    public IReadOnlyList<InventoryItem> GetAll()
    {
        using var db = new SmartSpiceContext();
        return db.InventoryItems
            .Include(i => i.Warehouse)
            .OrderBy(i => i.Category).ThenBy(i => i.SpiceName)
            .ToList();
    }

    public IReadOnlyList<InventoryItem> GetLowStock()
    {
        using var db = new SmartSpiceContext();
        return db.InventoryItems
            .Include(i => i.Warehouse)
            .Where(i => i.QuantityKg <= i.ReorderLevelKg)
            .OrderBy(i => i.QuantityKg)
            .ToList();
    }

    public decimal TotalStockValue()
    {
        using var db = new SmartSpiceContext();
        // Evaluated client-side because StockValue is a computed property.
        return db.InventoryItems.AsEnumerable().Sum(i => i.StockValue);
    }

    public double TotalWeight(InventoryCategory category)
    {
        using var db = new SmartSpiceContext();
        return db.InventoryItems.Where(i => i.Category == category).Sum(i => i.QuantityKg);
    }

    public void AdjustStock(int itemId, double deltaKg, string reason)
    {
        using var db = new SmartSpiceContext();
        var item = db.InventoryItems.Find(itemId)
            ?? throw new InvalidOperationException("Inventory item not found.");

        double newQty = item.QuantityKg + deltaKg;
        if (newQty < 0)
            throw new InvalidOperationException("Adjustment would make stock negative.");

        item.QuantityKg = newQty;
        item.LastUpdated = DateTime.Now;
        db.SaveChanges();

        _audit.Log("ADJUST_STOCK", "InventoryItem",
            $"{item.SpiceName} {deltaKg:+0.##;-0.##} kg ({reason}) → {newQty:N1} kg");

        _notifications.RefreshLowStockAlerts();
    }

    public void ReceiveStock(string spiceName, InventoryCategory category, int warehouseId, double kg, decimal unitPrice, string reason)
    {
        if (kg <= 0) return;
        using var db = new SmartSpiceContext();
        var item = db.InventoryItems.FirstOrDefault(i =>
            i.SpiceName == spiceName && i.Category == category && i.WarehouseId == warehouseId);

        if (item == null)
        {
            item = new InventoryItem
            {
                SpiceName = spiceName,
                Category = category,
                WarehouseId = warehouseId,
                QuantityKg = kg,
                ReorderLevelKg = Math.Round(kg * 0.2, 0),
                UnitPricePerKg = unitPrice,
                ExpiryDate = category == InventoryCategory.ProcessedPowder ? DateTime.Now.AddMonths(8) : null,
                LastUpdated = DateTime.Now
            };
            db.InventoryItems.Add(item);
        }
        else
        {
            item.QuantityKg += kg;
            if (unitPrice > 0) item.UnitPricePerKg = unitPrice;
            item.LastUpdated = DateTime.Now;
        }
        db.SaveChanges();
        _audit.Log("RECEIVE_STOCK", "InventoryItem", $"{spiceName} +{kg:N0} kg ({reason})");
        _notifications.RefreshLowStockAlerts();
    }

    public double IssueStock(string spiceName, InventoryCategory category, double kg, string reason)
    {
        if (kg <= 0) return 0;
        using var db = new SmartSpiceContext();
        var lines = db.InventoryItems
            .Where(i => i.SpiceName == spiceName && i.Category == category && i.QuantityKg > 0)
            .OrderByDescending(i => i.QuantityKg)
            .ToList();

        double remaining = kg;
        foreach (var line in lines)
        {
            if (remaining <= 0) break;
            double take = Math.Min(line.QuantityKg, remaining);
            line.QuantityKg -= take;
            line.LastUpdated = DateTime.Now;
            remaining -= take;
        }
        db.SaveChanges();

        double issued = kg - remaining;
        _audit.Log("ISSUE_STOCK", "InventoryItem", $"{spiceName} -{issued:N0} kg ({reason})");
        _notifications.RefreshLowStockAlerts();
        return issued;
    }

    public double IssueStockByName(string spiceName, double kg, string reason)
    {
        if (kg <= 0) return 0;
        using var db = new SmartSpiceContext();
        var lines = db.InventoryItems
            .Where(i => i.SpiceName == spiceName && i.QuantityKg > 0)
            .OrderByDescending(i => i.QuantityKg)
            .ToList();

        double remaining = kg;
        foreach (var line in lines)
        {
            if (remaining <= 0) break;
            double take = Math.Min(line.QuantityKg, remaining);
            line.QuantityKg -= take;
            line.LastUpdated = DateTime.Now;
            remaining -= take;
        }
        db.SaveChanges();

        double issued = kg - remaining;
        _audit.Log("ISSUE_STOCK", "InventoryItem", $"{spiceName} -{issued:N0} kg ({reason})");
        _notifications.RefreshLowStockAlerts();
        return issued;
    }

    public IReadOnlyList<string> SpiceNames()
    {
        using var db = new SmartSpiceContext();
        return db.InventoryItems.Select(i => i.SpiceName).Distinct().OrderBy(n => n).ToList();
    }

    public void Save(InventoryItem item)
    {
        using var db = new SmartSpiceContext();
        item.LastUpdated = DateTime.Now;
        if (item.Id == 0) db.InventoryItems.Add(item);
        else db.InventoryItems.Update(item);
        db.SaveChanges();
        _audit.Log(item.Id == 0 ? "CREATE" : "UPDATE", "InventoryItem", item.SpiceName);
        _notifications.RefreshLowStockAlerts();
    }

    public void Delete(int itemId)
    {
        using var db = new SmartSpiceContext();
        var item = db.InventoryItems.Find(itemId);
        if (item == null) return;
        db.InventoryItems.Remove(item);
        db.SaveChanges();
        _audit.Log("DELETE", "InventoryItem", item.SpiceName);
    }
}