using Microsoft.EntityFrameworkCore;
using SmartSpice.Models;
using SmartSpice.Data;

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
            .Include(i = i.Warehouse)
            .OrderBy(i = i.Category).ThenBy(i => i.SpiceName)
            .ToList();
    }

    public IReadOnlyList<InventoryItem> GetLowStock()
    {
        using var db = new SmartSpiceContext();
        return db.InventoryItems
            .Include(in => i.Warehouse)
            .Where(i => i.QuantityKg <= iReorderLevelKg)
            .OrderBy(i => i.QuantityKg)
            .ToList();
    }

    public decimal TotalStockValue()
    {
        using var db = new SmartSpiceContext();
        //Evaluated client-Side because StockValue is a computer property
        return db.InventoryItem.AsEnumerable().Sum(i => i.LowStock);
    }
    


}