using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using SmartSpice.Data;
using SmartSpice.Models;
using SmartSpice.Services;

namespace SmartSpice.ViewModels;

public partial class QualityViewModel : ViewModelBase, ISearchable
{
    private List<QualityInspection> _all = new();
    private string _search = string.Empty;

    public ObservableCollection<QualityInspection> Inspections { get; } = new();
    public ObservableCollection <SpiceBatch> Batches { get; } = new();   // eligible for inspection
    public Array Grades => Enum.GetValues(typeof(QualityGrade));

    [ObservableProperty] private bool _isCreating;
    [ObservableProperty] private bool _isDetail;
    [ObservableProperty] private QualityInspection? _detailInspection;

    [ObservableProperty] private int _totalInspections;
    [ObservableProperty] private int _passedCount;
    [ObservableProperty] private int _rejectedCount;
    [ObservableProperty] private int _exportCount;

    // New inspection form
    [ObservableProperty] private SpiceBatch? _formBatch;
    [ObservableProperty] private QualityGrade _formGrade = QualityGrade.A;
    [ObservableProperty] private double _formMoisture = 9;
    [ObservableProperty] private double _formPurity = 96;
    [ObservableProperty] private bool _formPassed = true;
    [ObservableProperty] private bool _formExport;
    [ObservableProperty] private string _formRemarks = string.Empty;

    // Previous inspection shown when re-inspecting a failed batch
    [ObservableProperty] private QualityInspection? _previousInspection;
    public bool HasPrevious => PreviousInspection != null;

    public QualityViewModel() => Title = "Quality Control";

    public override void Load() => Refresh();

    private void Refresh()
    {
        using var db = new SmartSpiceContext();
        _all = db.QualityInspections.Include(q => q.Batch).Include(q => q.Inspector)
            .OrderByDescending(q => q.InspectedAt).ToList();

        TotalInspections = _all.Count;
        PassedCount = _all.Count(i => i.PassedFoodSafety);
        RejectedCount = _all.Count(i => !i.PassedFoodSafety);
        ExportCount = _all.Count(i => i.ExportApproved);
        ApplyFilter();

        
        var latest = _all.GroupBy(i => i.BatchId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(i => i.InspectedAt).First());

        Batches.Clear();
        foreach (var b in db.SpiceBatches.OrderByDescending(b => b.CollectedDate).ToList())
            if (!latest.TryGetValue(b.Id, out var li) || !li.PassedFoodSafety)
                Batches.Add(b);

        FormBatch = Batches.FirstOrDefault();
    }

    public void ApplySearch(string? text)
    {
        _search = text?.Trim() ?? string.Empty;
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        IEnumerable<QualityInspection> q = _all;
        if (_search.Length > 0)
            q = q.Where(i =>
                i.CertificateNo.Contains(_search, StringComparison.OrdinalIgnoreCase) ||
                (i.Batch?.BatchCode.Contains(_search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                i.Grade.ToString().Contains(_search, StringComparison.OrdinalIgnoreCase) ||
                i.ResultText.Contains(_search, StringComparison.OrdinalIgnoreCase));

        Inspections.Clear();
        foreach (var i in q) Inspections.Add(i);
    }

    partial void OnFormBatchChanged(SpiceBatch? value)
        => PreviousInspection = value == null ? null : _all.FirstOrDefault(i => i.BatchId == value.Id);

    partial void OnPreviousInspectionChanged(QualityInspection? value)
        => OnPropertyChanged(nameof(HasPrevious));

    [RelayCommand] private void StartCreate() => IsCreating = true;
    [RelayCommand] private void CancelCreate() => IsCreating = false;

    [RelayCommand]
    private void OpenDetail(QualityInspection? inspection)
    {
        if (inspection == null) return;
        DetailInspection = inspection;
        IsDetail = true;
    }

    [RelayCommand] private void CloseDetail() => IsDetail = false;

    [RelayCommand]
    private void SaveInspection()
    {
        if (FormBatch == null)
        {
            MessageBox.Show("Select a batch to inspect.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        using (var db = new SmartSpiceContext())
        {
            int n = db.QualityInspections.Count() + 1;
            db.QualityInspections.Add(new QualityInspection
            {
                BatchId = FormBatch.Id,
                Grade = FormGrade,
                MoisturePercent = FormMoisture,
                PurityPercent = FormPurity,
                PassedFoodSafety = FormPassed,
                ExportApproved = FormExport,
                Remarks = FormRemarks,
                CertificateNo = $"QC-{DateTime.Now:yyyy}-{n:000}",
                InspectorEmployeeId = ServiceHub.Session.CurrentUser?.Id ?? 0
            });
            db.SaveChanges();
        }
        ServiceHub.Audit.Log("INSPECT", "QualityInspection", $"{FormBatch.BatchCode} graded {FormGrade}");
        IsCreating = false;
        FormRemarks = string.Empty;
        Refresh();
    }
}
