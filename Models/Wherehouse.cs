namespace SmartSpice.Models;

public class Warehouse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public double CapacityKg { get; set; }

    public List<InventoryItem> Items { get; set; } = new();

    public double UsedKg => Items?.Sum(i => i.QuantityKg) ?? 0;

    public double FreeKg => Math.Max(0, CapacityKg - UsedKg);

    public double UtilizationPercent =>
        CapacityKg <= 0 ? 0 : Math.Round(UsedKg / CapacityKg * 100, 1);

    public double FillPercent => Math.Min(100, Math.Max(0, UtilizationPercent));
    public double EmptyPercent => 100 - FillPercent;

    public int ItemCount => Items?.Count ?? 0;
}
