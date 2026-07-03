using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartSpice.Data;
using SmartSpice.Models;
using SmartSpice.Services;

namespace SmartSpice.ViewModels;

public partial class InventoryViewModel : ViewModelBase, ISearchable
{
    private readonly IInventoryService _inventory = ServiceHub.Inventory;
    private List<InventoryItem> _all = new();
    private string _search = string.Empty;

    public ObservableCollection<InventoryItem> Items { get; } = new();
    public ObservableCollection<Warehouse> Warehouses { get; } = new();
    public Array Categories => Enum.GetValues(typeof(InventoryCategory));

    public string[] Filters { get; } = { "All", "RawMaterial", "ProcessedPowder", "Packaging" };
    [ObservableProperty] private string _activeFilter = "All";

    [ObservableProperty] private InventoryItem? _selected;
    [ObservableProperty] private bool _isEditing;

    // stat-card metrics
    [ObservableProperty] private string _totalStock = "0";
    [ObservableProperty] private int _lowStockCount;
    [ObservableProperty] private int _outOfStockCount;
    [ObservableProperty] private int _warehouseCount;

    public InventoryViewModel() => Title = "Inventory";

    public override void Load()
    {
        using (var db = new SmartSpiceContext())
        {
            Warehouses.Clear();
            foreach (var w in db.Warehouses.ToList()) Warehouses.Add(w);
        }
        Refresh();
    }

    private void Refresh()
    {
        _all = _inventory.GetAll().ToList();
        WarehouseCount = Warehouses.Count;
        TotalStock = $"{_all.Sum(i => i.QuantityKg):N0}";
        LowStockCount = _all.Count(i => i.IsLowStock);
        OutOfStockCount = _all.Count(i => i.QuantityKg <= 0);
        ApplyFilter();
    }

    public void ApplySearch(string? text)
    {
        _search = text?.Trim() ?? string.Empty;
        ApplyFilter();
    }

    partial void OnActiveFilterChanged(string value) => ApplyFilter();

    [RelayCommand]
    private void SetFilter(string filter) => ActiveFilter = filter;

    private void ApplyFilter()
    {
        IEnumerable<InventoryItem> q = _all;
        if (ActiveFilter != "All" && Enum.TryParse<InventoryCategory>(ActiveFilter, out var cat))
            q = q.Where(i => i.Category == cat);
        if (_search.Length > 0)
            q = q.Where(i =>
                i.SpiceName.Contains(_search, StringComparison.OrdinalIgnoreCase) ||
                i.Category.ToString().Contains(_search, StringComparison.OrdinalIgnoreCase) ||
                (i.Warehouse?.Name.Contains(_search, StringComparison.OrdinalIgnoreCase) ?? false));

        Items.Clear();
        foreach (var i in q) Items.Add(i);
    }

    [RelayCommand]
    private void New()
    {
        Selected = new InventoryItem
        {
            Category = InventoryCategory.RawMaterial,
            WarehouseId = Warehouses.FirstOrDefault()?.Id ?? 0
        };
        IsEditing = true;
    }

    [RelayCommand]
    private void EditRow(InventoryItem? item)
    {
        if (item == null) return;
        Selected = Clone(item);
        IsEditing = true;
    }

    [RelayCommand]
    private void Save()
    {
        if (Selected == null) return;
        if (string.IsNullOrWhiteSpace(Selected.SpiceName))
        {
            MessageBox.Show("Spice name is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        try
        {
            _inventory.Save(Selected);
            IsEditing = false;
            Selected = null;
            Refresh();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        IsEditing = false;
        Selected = null;
    }

    [RelayCommand]
    private void Delete()
    {
        if (Selected == null || Selected.Id == 0) return;
        if (MessageBox.Show($"Delete '{Selected.SpiceName}'?", "Confirm",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        _inventory.Delete(Selected.Id);
        IsEditing = false;
        Selected = null;
        Refresh();
    }

    private static InventoryItem Clone(InventoryItem s) => new()
    {
        Id = s.Id,
        SpiceName = s.SpiceName,
        Category = s.Category,
        QuantityKg = s.QuantityKg,
        ReorderLevelKg = s.ReorderLevelKg,
        UnitPricePerKg = s.UnitPricePerKg,
        ExpiryDate = s.ExpiryDate,
        WarehouseId = s.WarehouseId
    };
}