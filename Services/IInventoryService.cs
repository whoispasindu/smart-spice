using SmartSpice.Models;

namespace SmartSpice.Services;


public interface IInventoryService
{
    IReadOnlyList<InventoryItem> GetAll();
    IReadOnlyList<InventoryItem> GetLowStock();
    decimal TotalStockValue();
    double TotalWeight(InventoryCategory category);

    void AdjustStock(int itemId, double deltaKg, string reason);


    void ReceiveStock(string spiceName, InventoryCategory category, int warehouseId, double kg, decimal unitPrice, string reason);


    double IssueStock(string spiceName, InventoryCategory category, double kg, string reason);


    double IssueStockByName(string spiceName, double kg, string reason);

    IReadOnlyList<string> SpiceNames();

    void Save(InventoryItem item);
    void Delete(int itemId);
}
