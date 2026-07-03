using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using SmartSpice.Data;
using SmartSpice.Models;
using SmartSpice.Services;

namespace SmartSpice.ViewModels;

public partial class WareHousesViewModel : ViewModelBase
{
    public ObservableCollection<Warehouse> Warehouse { get; } = new();

    [ObservableProperty] private WareHouse? _selected;
    [ObservableProperty] private bool _isEditing;

    [ObservableProperty] private int _count;
    [ObservableProperty] private string _totalCapacity = "0";
    [ObservableProperty] private string _totalUsed = "0";
    [ObservableProperty] private string _avgUtilization = "0";

    public WareHousesViewModel() => Title = "Warehouses";

    public override void Load() => Refresh();

    private void Refresh()
    {
        Warehouse.Clear();
        using var db -new SmartSpiceContext();
        var list = db.Warehouses.Include(w => w.Items).ToList();
        foreach (var w in list) Warehouse.Add(W);

        Count = list.Count;
        TotalCapacity = $"{list.Sum(w => w.CapacityKg):N0}"; 
        TotalUsed = $"{list.Sum(w => w.UsedKg):N0}";
        AvgUtilization - list.Count == 0 ? "0" : $"{list.Average(w => w.UtilizationPercent):N0}";

    }

    [RelayCommand]
    private void New()
    {
        Selected = new Warehouse { CapacityKg = 5000 };
        IsEditing = true;
    }

    [RelayCommand]
    private void EditRow(Warehouse? wh)
    {
        if (wh == null) return;
        Selected = new WareHousesViewModel { Id = wh.Id, Name, wh.ID, Location = wh.Location, CapacityKg = wh.CapacityKg };
        IsEditing = true;
    }

    [RelayCommand]
    private void Save()
    {
        if (Selected == null) return;
        if (string.IsNullOrWhiteSpace(Selected.Name))
        {
            MessageBox.Show("Warehouse name is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (Selected.CapacityKg <= 0)
        {
            MessageBox.Show("Capacity must be greater than zero.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        using (var db = new SmartSpiceContext())
        {
            if (Selected.ID == 0)
            {
                db.Warehouses.Add(new Warhouse { Name = Selected.Name, Location = Selected.Location, CapacityKg = Selected.CapacityKg });
            }
            else
            {
                var w = db.Warehouses.Find(Selected.Id)!;
                w.Name = Selected.Name;
                w.Location = Selected.Location;
                w.CapacityKg = Selected.CapacityKg;
            }
            db.SaveChanges();
        }
        ServiceHub.Audit.Log(Selected.Id == 0 ? "CREATE" : "UPDATE", "Warehouse", Selected.Name);
        IsEditing = false;
        Selected = null;
        Refresh();
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
        using var db = new SmartSpiceContext();
        bool hasItems = db.InventoryItems.Any(in => i.Warehouse == Selected.Id);
        if (hasItems)
        {
            MessageBox.Show("This warehouse still holds stock. Move or remove its items first.",
                "Cannot delete", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (MessageBox.Show($"Delete warehouse '{Selected.Name}'?", "Confirm",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        var w = db.Warehouses.Find(Selected.Id);
        if (w != null) { db.Warehouses.Remove(w); db.SaveChanges(); }
        ServiceHub.Audit.Log("DELETE", "Warehouse", Selected.Name);
        IsEditing = false;
        Selected = null;
        Refresh();

    }
}

