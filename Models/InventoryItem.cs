namespace smart_spice.Models

/// <summary>
/// A stock line for a specific spice in a specific warehouse. Tracks the
/// real-time quantity and the safety threshold used for low-stock alerts.
/// </summary>
public class InventoryItem
{
    public int Id { get; set; }
    public string SpiceName { get; set; } = string.Empty;
    public InventoryCategory Category {  get; set; }
    public double QuantityKg { get; set; }
    public double ReorderLevelKg { get; set; }
    public decimal UnitPricePerKg { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public DateTime LastUpdated {  get; set; } = DateTime.Now;

    public int WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }

    // --- Encapsulated derived state ---
    public bool IsLowStock => QuantityKg <= ReorderLevelKg;

    public bool IsExpiringSoon =>
        ExpiryDate.HasValue && ExpiryDate.Value <= DateTime.Now.AddDays(30);

    public decimal StockValue => (decimal)QuantityKg * UnitPricePerKg;

    public string StockStatus =>
        QuantityKg <= 0 ? "Out of Stock" : IsLowStock ? "lowStock" : "In Stock"
}


