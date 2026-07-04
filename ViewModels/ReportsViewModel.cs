using System.Collections.ObjectModel;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using SmartSpice.Data;
using SmartSpice.Helpers;
using SmartSpice.Models;
using SmartSpice.Services;

namespace SmartSpice.ViewModels;

// ---------- report row models ----------
public class ReportMetric
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Sub { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public Brush Accent { get; set; } = Brushes.Green;
}

public class ProductionRow
{
    public string Spice { get; set; } = string.Empty;
    public int Batches { get; set; }
    public double RawKg { get; set; }
    public double ProcessedKg { get; set; }
    public double YieldLossPct { get; set; }
}

public class BuyerSalesRow
{
    public string Buyer { get; set; } = string.Empty;
    public string Market { get; set; } = string.Empty;
    public int Orders { get; set; }
    public decimal Revenue { get; set; }
}

public class ValuationRow
{
    public string Category { get; set; } = string.Empty;
    public double Kg { get; set; }
    public decimal Value { get; set; }
}

public class GradeRow
{
    public string Grade { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Fraction { get; set; }
    public Brush Brush { get; set; } = Brushes.Green;
}

public partial class ReportsViewModel : ViewModelBase
{
    public ObservableCollection<ReportMetric> Summary { get; } = new();
    public ObservableCollection<ProductionRow> Production { get; } = new();
    public ObservableCollection<BuyerSalesRow> Sales { get; } = new();
    public ObservableCollection<ValuationRow> Valuation { get; } = new();
    public ObservableCollection<GradeRow> Grades { get; } = new();
    public ObservableCollection<AuditLog> Logs { get; } = new();

    public string GeneratedAt => $"Generated {DateTime.Now:MMMM d, yyyy  h:mm tt}";

    public ReportsViewModel() => Title = "Reports & Analytics";

    public override void Load()
    {
        using var db = new SmartSpiceContext();
        BuildSummary(db);
        BuildProduction(db);
        BuildSales(db);
        BuildValuation(db);
        BuildGrades(db);

        Logs.Clear();
        foreach (var l in db.AuditLogs.OrderByDescending(l => l.Timestamp).Take(200).ToList())
            Logs.Add(l);
    }

    private void BuildSummary(SmartSpiceContext db)
    {
        double processed = db.SpiceBatches.AsEnumerable().Sum(b => b.ProcessedWeightKg ?? 0);
        decimal revenue = db.Orders.Include(o => o.Items).AsEnumerable().Sum(o => o.TotalAmount);
        decimal stockValue = db.InventoryItems.AsEnumerable().Sum(i => i.StockValue);
        var insp = db.QualityInspections.ToList();
        double pass = insp.Count == 0 ? 0 : Math.Round(insp.Count(i => i.PassedFoodSafety) * 100.0 / insp.Count, 0);

        Brush g = new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32));
        Brush y = new SolidColorBrush(Color.FromRgb(0xF4, 0xC0, 0x33));
        Brush b = new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x9A));
        Brush o = new SolidColorBrush(Color.FromRgb(0xEF, 0x8A, 0x1F));

        Summary.Clear();
        Summary.Add(new ReportMetric { Label = "Total Production", Value = $"{processed:N0} KG", Sub = "Processed output", Icon = "⚙", Accent = g });
        Summary.Add(new ReportMetric { Label = "Total Revenue", Value = $"LKR {revenue:N0}", Sub = "All orders", Icon = "💰", Accent = y });
        Summary.Add(new ReportMetric { Label = "Inventory Value", Value = $"LKR {stockValue:N0}", Sub = "Current stock", Icon = "📦", Accent = b });
        Summary.Add(new ReportMetric { Label = "QC Pass Rate", Value = $"{pass:N0}%", Sub = $"{insp.Count} inspections", Icon = "✅", Accent = o });
    }

    private void BuildProduction(SmartSpiceContext db)
    {
        var rows = db.SpiceBatches.AsEnumerable()
            .GroupBy(b => b.SpiceType)
            .Select(grp =>
            {
                double raw = grp.Sum(b => b.RawWeightKg);
                double proc = grp.Sum(b => b.ProcessedWeightKg ?? 0);
                double processedRaw = grp.Where(b => b.ProcessedWeightKg.HasValue).Sum(b => b.RawWeightKg);
                double loss = processedRaw > 0 ? Math.Round((processedRaw - proc) / processedRaw * 100, 1) : 0;
                return new ProductionRow { Spice = grp.Key, Batches = grp.Count(), RawKg = raw, ProcessedKg = proc, YieldLossPct = loss };
            })
            .OrderByDescending(r => r.RawKg);

        Production.Clear();
        foreach (var r in rows) Production.Add(r);
    }

    private void BuildSales(SmartSpiceContext db)
    {
        var rows = db.Orders.Include(o => o.Buyer).Include(o => o.Items).AsEnumerable()
            .GroupBy(o => o.Buyer)
            .Select(grp => new BuyerSalesRow
            {
                Buyer = grp.Key?.CompanyName ?? "—",
                Market = (grp.Key?.IsExport ?? false) ? "Export" : "Local",
                Orders = grp.Count(),
                Revenue = grp.Sum(o => o.TotalAmount)
            })
            .OrderByDescending(r => r.Revenue);

        Sales.Clear();
        foreach (var r in rows) Sales.Add(r);
    }

    private void BuildValuation(SmartSpiceContext db)
    {
        var rows = db.InventoryItems.AsEnumerable()
            .GroupBy(i => i.Category)
            .Select(grp => new ValuationRow
            {
                Category = grp.Key.ToString(),
                Kg = grp.Sum(i => i.QuantityKg),
                Value = grp.Sum(i => i.StockValue)
            })
            .OrderByDescending(r => r.Value);

        Valuation.Clear();
        foreach (var r in rows) Valuation.Add(r);
    }

    private void BuildGrades(SmartSpiceContext db)
    {
        var insp = db.QualityInspections.ToList();
        int total = Math.Max(1, insp.Count);
        var palette = new Dictionary<QualityGrade, Brush>
        {
            [QualityGrade.A] = new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32)),
            [QualityGrade.B] = new SolidColorBrush(Color.FromRgb(0x7C, 0xB3, 0x42)),
            [QualityGrade.C] = new SolidColorBrush(Color.FromRgb(0xF4, 0xC0, 0x33)),
            [QualityGrade.D] = new SolidColorBrush(Color.FromRgb(0xEF, 0x8A, 0x1F)),
            [QualityGrade.Rejected] = new SolidColorBrush(Color.FromRgb(0xD1, 0x43, 0x43)),
        };

        Grades.Clear();
        foreach (QualityGrade g in Enum.GetValues<QualityGrade>())
        {
            int count = insp.Count(i => i.Grade == g);
            Grades.Add(new GradeRow
            {
                Grade = g == QualityGrade.Rejected ? "Rejected" : $"Grade {g}",
                Count = count,
                Fraction = (double)count / total,
                Brush = palette[g]
            });
        }
    }

    [RelayCommand]
    private void ExportProduction()
    {
        CsvExporter.Export("production-report.csv",
            new[] { "Spice", "Batches", "Raw (kg)", "Processed (kg)", "Yield Loss %" },
            Production.Select(r => (IList<string>)new[]
            {
                r.Spice, r.Batches.ToString(), r.RawKg.ToString("N0"),
                r.ProcessedKg.ToString("N0"), r.YieldLossPct.ToString("N1")
            }));
    }

    [RelayCommand]
    private void ExportSales()
    {
        CsvExporter.Export("sales-report.csv",
            new[] { "Buyer", "Market", "Orders", "Revenue (LKR)" },
            Sales.Select(r => (IList<string>)new[]
            {
                r.Buyer, r.Market, r.Orders.ToString(), r.Revenue.ToString("N0")
            }));
    }

    [RelayCommand]
    private void ExportAudit()
    {
        CsvExporter.Export("audit-log.csv",
            new[] { "Timestamp", "User", "Action", "Entity", "Details" },
            Logs.Select(l => (IList<string>)new[]
            {
                l.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"), l.Username, l.Action, l.Entity, l.Details
            }));
    }
}
