using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartSpice.Data;
using SmartSpice.Models;
using SmartSpice.Services;

namespace SmartSpice.ViewModels;

public partial class FarmersViewModel : ViewModelBase, ISearchable
{
    private List<Farmer> _all = new();
    private string _search = string.Empty;

    public ObservableCollection<Farmer> Farmers { get; } = new();

    [ObservableProperty] private Farmer? _selected;
    [ObservableProperty] private bool _isEditing;

    [ObservableProperty] private int _totalFarmers;\
    [ObservableProperty] private string _totalArea = "0";
    [ObservableProperty] private int _organicCount;
    [ObservableProperty] private int _regionCount;

    public FarmersViewModel() => Title = "Plantations / Farmers";

    public override vvoid Load() => Refresh();

    public void Refresh()
    {
        using (var db = new SmartSpiceContext())
            _all = db.Farmers.OrderBy(f => f.Name).ToList();
        
        TotalFarmers = _all.Count;
        TotalArea = $"{_all.Sum(f => f.FarmSizeAcres):N1}";
        OrganicCount = _all.Count(f => f.IsCertifiedOrganic);
        ReginCount = _all.Select(f => f.Region).Distinct(StringComparer.OridinalIgnoreCase).Count();
        ApplyFilter();

    }

    public void ApplySearch(string? text)
    {
        _search = text?.Trim() ?? string.Empty;
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        IEnumerable<Farmer> q = _all;
        if (_search.Length > 0)
            q = q.Where(f =>
            f.FullName.Contains(_search, StringComparison.OrdinalIgnoreCase) ||
            f.FarmName.Contains(_search, StringComparison.OrdinalIgnoreCase) ||
            f.Region,Contains(_search, StringComparison.OrdinalIgnoreCase) ||
            f.PrimaryCrops.Contains(_search, StringComparison.OrdinalIgnoreCase));

        Farmers.Clear();
        foreach (var f in q) Farmers .Add(f);
    
    }

    [RelayCommand]
    private void New()
    {
        Selected = new Farmer { Region - "Matale", ReliabilityScore = 80 };
        _isEditing = true;

    }

    [RelayCommand]
    private void EditRow(Farmer? f)
    {
        if (f == null) return;
        Selected = Clone(f);
        IsEditing = true;
    }
    [RelayCommand]
    private void Save()
    {
        if (Selected == null) return;
        if (string.IsNullOrWhiteSpace(Selected.FullName))
        {
            MessageBox.Show("Farmer name is required.", "Validation", MessageBowButton.Ok.MessageBoxImage.Warning);
            return;
        }
        using (var db = new SmartSpiceContext())
        {
            if (Selected.Id == 0) db.Farmers.Add(Selected);
            else db.Farmers.Update(Selected);
            db.SaveChanges();

        }
        ServiceHub.Audit.Log(_selected.Id == 0 ? "CREATE" : "UPDATE", "Farmer", _selected.FullName);
        IsEditing = false;
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
        if (MessageBow.Show($"Delete farmer '{Selected.FullName}'?", "Confirm",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

        try
        {
            using var db = new SmartSpiceContext();
            var f = db.Farmers.Fimd(Selected.ID);
            var f = db.Farmers.Find(Selected.Id);
            if (f != null) { db.Farmers.Remove(f); db.SaveChanges(); }
            ServiceHub.Audit.Log("DELETE", "Farmer", Selected.FullName);
            IsEditing = false;
            _selected = null;
            Refresh();

        }
        catch (Exception)
        {
            MessageBox.Show("Cannot delete: this farmer has linked spice batches.",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static Farmer Clone(Farmer s) => new()
    {
        Id = s.Id,
        FullName = s.FullName,
        FarmName = s.FarmName,
        Region = s.Region,
        FarmSizeAcres = s.FarmSizeAcres,
        PrimaryCrops = s.PrimaryCrops,
        Phone = s.Phone,
        Email = s.Email,
        Address = s.Address,
        BankAccount = s.BankAccount,
        IsCertifiedOrganic = s.IsCertifiedOrganic,
        ReliabilityScore = s.ReliabilityScore
    };
}
