using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using SmartSpice.Data;
using SmartSpice.Helpers;
using SmartSpice.Models;
using SmartSpice.Services;

namespace SmartSpice.ViewModels;

public partial class BatchesViewModel : ViewModelBase, ISearchable
{
    private List<SpiceBatch> _all = new();
    private string _search = string.Empty;

    public ObservableCollection<SpiceBatch> Batches { get; } = new();
    public ObservableCollection<ProcessingRecord> Records { get; } = new();
    public ObservableCollection<Farmer> Farmers { get; } = new();
    public ObservableCollection<Warehouse> Warehouses { get; } = new();

    /// <summary>Only these fixed products may be added (no free-typed names).</summary>
    public IReadOnlyList<string> RawProducts => SpiceCatalog.RawProducts;

    [ObservableProperty] private SpiceBatch? _selected;
    [ObservableProperty] private bool _isCreating;
    [ObservableProperty] private bool _isDetail;

    // stat metrics
    [ObservableProperty] private int _totalBatches;
    [ObservableProperty] private int _activeBatches;
    [ObservableProperty] private string _rawTotal = "0";
    [ObservableProperty] private string _avgYield = "0";

    // New-batch form
    [ObservableProperty] private string _newSpiceType = "Dried Red Chillies";
    [ObservableProperty] private double _newRawWeight = 300;
    [ObservableProperty] private double _newMoisture = 12;
    [ObservableProperty] private double _newDryingHours = 24;
    [ObservableProperty] private Farmer? _newFarmer;
    [ObservableProperty] private Warehouse? _newWarehouse;

    // Grinding form
    [ObservableProperty] private double _processedWeight;

    public BatchesViewModel() => Title = "Harvest & Processing";

    public override void Load()
    {
        using (var db = new SmartSpiceContext())
        {
            Farmers.Clear();
            foreach (var f in db.Farmers.OrderBy(f => f.FullName).ToList()) Farmers.Add(f);
            NewFarmer = Farmers.FirstOrDefault();

            Warehouses.Clear();
            foreach (var w in db.Warehouses.ToList()) Warehouses.Add(w);
            NewWarehouse = Warehouses.FirstOrDefault();
        }
        NewSpiceType = RawProducts.FirstOrDefault() ?? "Cinnamon";
        Refresh();
    }

    private decimal PriceFor(string spice, InventoryCategory category)
    {
        var match = ServiceHub.Inventory.GetAll()
            .FirstOrDefault(i => i.SpiceName == spice && i.Category == category)
            ?? ServiceHub.Inventory.GetAll().FirstOrDefault(i => i.SpiceName == spice);
        return match?.UnitPricePerKg ?? 800m;
    }

    private void Refresh()
    {
        using (var db = new SmartSpiceContext())
            _all = db.SpiceBatches.Include(x => x.Farmer)
                .OrderByDescending(x => x.CollectedDate).ToList();

        TotalBatches = _all.Count;
        ActiveBatches = _all.Count(b => b.Status != BatchStatus.Dispatched);
        RawTotal = $"{_all.Sum(b => b.RawWeightKg):N0}";
        var graded = _all.Where(b => b.YieldPercent.HasValue).ToList();
        AvgYield = graded.Count == 0 ? "—" : $"{graded.Average(b => b.YieldPercent!.Value):N1}";
        ApplyFilter();
    }

    public void ApplySearch(string? text)
    {
        _search = text?.Trim() ?? string.Empty;
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        IEnumerable<SpiceBatch> q = _all;
        if (_search.Length > 0)
            q = q.Where(b =>
                b.BatchCode.Contains(_search, StringComparison.OrdinalIgnoreCase) ||
                b.SpiceType.Contains(_search, StringComparison.OrdinalIgnoreCase) ||
                b.Status.ToString().Contains(_search, StringComparison.OrdinalIgnoreCase) ||
                (b.Farmer?.FarmName.Contains(_search, StringComparison.OrdinalIgnoreCase) ?? false));

        Batches.Clear();
        foreach (var b in q) Batches.Add(b);
    }

    private void LoadRecords()
    {
        Records.Clear();
        if (Selected == null) return;
        using var db = new SmartSpiceContext();
        foreach (var r in db.ProcessingRecords.Where(r => r.BatchId == Selected.Id)
                     .OrderBy(r => r.PerformedAt).ToList())
            Records.Add(r);
        ProcessedWeight = Selected.ProcessedWeightKg ?? Math.Round(Selected.RawWeightKg * 0.88, 1);
    }

    [RelayCommand] private void StartCreate() => IsCreating = true;
    [RelayCommand] private void CancelCreate() => IsCreating = false;

    [RelayCommand]
    private void OpenDetail(SpiceBatch? batch)
    {
        if (batch == null) return;
        Selected = batch;
        LoadRecords();
        IsDetail = true;
    }

    [RelayCommand] private void CloseDetail() => IsDetail = false;

    [RelayCommand]
    private void CreateBatch()
    {
        if (NewFarmer == null || string.IsNullOrWhiteSpace(NewSpiceType) || NewRawWeight <= 0)
        {
            MessageBox.Show("Choose a farmer, spice type and a positive raw weight.",
                "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        string code;
        int whId = NewWarehouse?.Id ?? Warehouses.FirstOrDefault()?.Id ?? 0;
        string spice = NewSpiceType.Trim();
        using (var db = new SmartSpiceContext())
        {
            int next = db.SpiceBatches.Count() + 1;
            code = $"BATCH-2026-{next:000}";
            db.SpiceBatches.Add(new SpiceBatch
            {
                BatchCode = code,
                SpiceType = spice,
                FarmerId = NewFarmer.Id,
                RawWeightKg = NewRawWeight,
                MoisturePercent = NewMoisture,
                DryingHours = NewDryingHours,
                Status = BatchStatus.Collected,
                WarehouseId = whId,
                AssignedEmployeeId = ServiceHub.Session.CurrentUser?.Id
            });
            db.SaveChanges();
        }
        ServiceHub.Audit.Log("CREATE", "SpiceBatch", $"{spice} {NewRawWeight} kg → {code}");

        // Harvested raw material enters inventory + warehouse stock automatically.
        ServiceHub.Inventory.ReceiveStock(spice, InventoryCategory.RawMaterial, whId,
            NewRawWeight, PriceFor(spice, InventoryCategory.RawMaterial), $"Harvest {code}");

        IsCreating = false;
        Load();
    }

    [RelayCommand]
    private void AdvanceStage()
    {
        if (Selected == null) return;
        // Harvest & Processing only runs up to Stored — dispatch happens in Dispatch Products.
        if (Selected.Status >= BatchStatus.Stored)
        {
            MessageBox.Show("This batch is already stored. Dispatching is done from Dispatch Products.", "Info");
            return;
        }

        int id = Selected.Id;
        bool nowStored = false;
        string raw = string.Empty, powder = string.Empty;
        double rawKg = 0, powderKg = 0;
        int whId = 0;
        using (var db = new SmartSpiceContext())
        {
            var b = db.SpiceBatches.Find(id)!;
            b.Status = (BatchStatus)((int)b.Status + 1);
            if (b.Status == BatchStatus.Stored)
            {
                b.CompletedDate = DateTime.Now;
                b.ProcessedWeightKg ??= Math.Round(b.RawWeightKg * 0.9, 1);  // default yield if not ground
                nowStored = true;
                raw = b.SpiceType;
                powder = SpiceCatalog.PowderFor(b.SpiceType);
                rawKg = b.RawWeightKg;
                powderKg = b.ProcessedWeightKg!.Value;
                whId = b.WarehouseId ?? Warehouses.FirstOrDefault()?.Id ?? 0;
            }
            db.SaveChanges();
            ServiceHub.Audit.Log("ADVANCE", "SpiceBatch", $"{b.BatchCode} → {b.Status}");
        }

        // On reaching Stored, the raw batch is converted into its finished powder in inventory.
        if (nowStored)
        {
            ServiceHub.Inventory.IssueStock(raw, InventoryCategory.RawMaterial, rawKg, "Processed to powder");
            ServiceHub.Inventory.ReceiveStock(powder, InventoryCategory.ProcessedPowder, whId,
                powderKg, PriceFor(powder, InventoryCategory.ProcessedPowder), "Stored from batch");
            ServiceHub.Audit.Log("STORE", "InventoryItem", $"{raw} → {powderKg:N0} kg {powder}");
        }

        Refresh();
        Selected = Batches.FirstOrDefault(b => b.Id == id);
        LoadRecords();
    }

    [RelayCommand]
    private void RecordGrinding()
    {
        if (Selected == null) return;
        if (ProcessedWeight <= 0 || ProcessedWeight > Selected.RawWeightKg)
        {
            MessageBox.Show("Processed weight must be positive and not exceed the raw weight.",
                "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        int id = Selected.Id;
        using (var db = new SmartSpiceContext())
        {
            var b = db.SpiceBatches.Find(id)!;
            b.ProcessedWeightKg = ProcessedWeight;
            if (b.Status < BatchStatus.Grinding) b.Status = BatchStatus.Grinding;
            db.ProcessingRecords.Add(new ProcessingRecord
            {
                BatchId = b.Id,
                Stage = BatchStatus.Grinding,
                Method = "Mechanical Mill",
                WeightBeforeKg = b.RawWeightKg,
                WeightAfterKg = ProcessedWeight,
                OperatorEmployeeId = ServiceHub.Session.CurrentUser?.Id ?? 0,
                Notes = "Grinding recorded via app"
            });
            db.SaveChanges();
            ServiceHub.Audit.Log("GRIND", "SpiceBatch",
                $"{b.BatchCode}: {b.RawWeightKg}→{ProcessedWeight} kg (loss {b.YieldLossPercent}%)");
        }
        // The powder enters inventory automatically when the batch reaches the Stored stage.

        Refresh();
        Selected = Batches.FirstOrDefault(b => b.Id == id);
        LoadRecords();
    }
}

