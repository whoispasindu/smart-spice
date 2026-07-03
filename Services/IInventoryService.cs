using SmartSpice.Models;

namespace SmartSpice.Services;

/// <summary>
/// Contract for stock operations. Demonstrates the interface-driven design the
/// proposal calls out (IInventoryService) for decoupling and testability.
/// </summary>
public interface IInventoryService
{
    IReadOnlyList<InventoryItem> GetAll();
    IReadOnlyList<InventoryItem> GetLowStock();
    decimal TotalStockValue();
    double TotalWeight(InventoryCategory category);

    /// <summary>Adjust a stock line by a (signed) delta, writing an audit entry.</summary>
    void AdjustStock(int itemId, double deltaKg, string reason);

    /// <summary>
    /// Add stock for a spice into a warehouse/category, creating the line if needed.
    /// Used when a harvested batch enters or grinding produces powder.
    /// </summary>
    void ReceiveStock(string spiceName, InventoryCategory category, int warehouseId, double kg, decimal unitPrice, string reason);

    /// <summary>
    /// Remove stock for a spice+category across warehouses (highest-stock first).
    /// Returns the quantity actually issued. Used when an order is exported.
    /// </summary>
    double IssueStock(string spiceName, InventoryCategory category, double kg, string reason);

    /// <summary>
    /// Remove stock by product name across any category (raw or powder).
    /// Used when dispatching an order that may contain raw materials or powders.
    /// </summary>
    double IssueStockByName(string spiceName, double kg, string reason);

    /// <summary>Distinct spice names already in inventory (for pickers).</summary>
    IReadOnlyList<string> SpiceNames();

    void Save(InventoryItem item);
    void Delete(int itemId);
}
