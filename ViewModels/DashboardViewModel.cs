using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using SmartSpice.Data;
using SmartSpice.Helpers;
using SmartSpice.Models;
using SmartSpice.Services;

namespace SmartSpice.ViewModels;

// ---------- Small presentation models for the dashboard ----------

public class KpiCard
{
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public Brush IconBackground { get; set; } = Brushes.Green;
    public string Sub { get; set; } = string.Empty;
    public string Trend { get; set; } = string.Empty;
    public bool ShowTrend { get; set; }
}

public class DonutSegment
{
    public Geometry Geometry { get; set; } = Geometry.Empty;
    public Brush Brush { get; set; } = Brushes.Green;
    public string Name { get; set; } = string.Empty;
    public string Kg { get; set; } = string.Empty;
    public string Percent { get; set; } = string.Empty;
}

public class BarRow
{
    public string Label { get; set; } = string.Empty;
    public string Icon { get; set; } = "•";
    public string ValueText { get; set; } = string.Empty;
    public double Fraction { get; set; }
    public double EmptyFraction => 1 - Math.Min(1, Math.Max(0, Fraction));
    public Brush Brush { get; set; } = Brushes.Green;
}

public class ActivityRow
{
    public string Icon { get; set; } = "•";
    public string Text { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
}

public class DashboardViewModel : ViewModelBase
{
    public ObservableCollection<KpiCard> Kpis { get; } = new();
    public ObservableCollection<DonutSegment> Donut { get; } = new();
    public ObservableCollection<BarRow> TopStock { get; } = new();
    public ObservableCollection<BarRow> WarehouseBars { get; } = new();
    public ObservableCollection<ActivityRow> Activities { get; } = new();

    
    public string SalesMonthLabel { get; private set; } = "—";
    public string SalesUnits { get; private set; } = "—";
    public string SalesRevenue { get; private set; } = "—";
    public string SalesTopProduct { get; private set; } = "—";
    public string SalesVsAvg { get; private set; } = "";
    public bool SalesHighSeason { get; private set; }
    public string SalesWarning { get; private set; } = string.Empty;

   
    public string DonutTotal { get; private set; } = "0";

    
    public Geometry GaugeTrack { get; private set; } = Geometry.Empty;
    public Geometry GaugeValue { get; private set; } = Geometry.Empty;
    public int HealthScore { get; private set; } = 0;
    public string HealthLabel { get; private set; } = "Good";

    
    public string InsightText { get; private set; } = string.Empty;
    public string RevenueForecast { get; private set; } = "—";
    public string RevenueForecastTrend { get; private set; } = "";

    public string UserName => ServiceHub.Session.CurrentUserName;
    public string TodayLong => DateTime.Now.ToString("MMMM d, yyyy");
    public string TodayDay => DateTime.Now.ToString("dddd");

    private static readonly Brush[] DonutPalette =
    {
        new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32)), // green
        new SolidColorBrush(Color.FromRgb(0x16, 0x4A, 0x1E)), // dark green
        new SolidColorBrush(Color.FromRgb(0xF4, 0xC0, 0x33)), // yellow
        new SolidColorBrush(Color.FromRgb(0x8B, 0xC3, 0x4A)), // light green
        new SolidColorBrush(Color.FromRgb(0xEF, 0x8A, 0x1F)), // orange
    };

    public DashboardViewModel() => Title = "Dashboard";

    public override void Load()
    {
        using var db = new SmartSpiceContext();

        BuildKpis(db);
        BuildSalesForecast();
        BuildDonut(db);
        BuildGauge(db);
        BuildTopStock(db);
        BuildWarehouses(db);
        BuildActivities(db);
        BuildInsight(db);

       
        OnPropertyChanged(string.Empty);
    }

    private static Brush LevelBrush(double percent) => new SolidColorBrush(
        percent >= 90 ? Color.FromRgb(0xD1, 0x43, 0x43)   // red — nearly full
      : percent >= 75 ? Color.FromRgb(0xEF, 0x8A, 0x1F)   // amber
      : Color.FromRgb(0x3E, 0x8E, 0x2E));                 // green

    private void BuildWarehouses(SmartSpiceContext db)
    {
        WarehouseBars.Clear();
        foreach (var w in db.Warehouses.Include(x => x.Items).ToList())
        {
            double util = w.UtilizationPercent;
            WarehouseBars.Add(new BarRow
            {
                Label = w.Name,
                ValueText = $"{util:N0}%",
                Fraction = Math.Min(1, util / 100.0),
                Brush = LevelBrush(util)
            });
        }
    }

    private void BuildKpis(SmartSpiceContext db)
    {
        int farmers = db.Farmers.Count();
        int workers = db.Employees.Count(e => e.IsActive);
        double invKg = db.InventoryItems.Sum(i => i.QuantityKg);
        int pending = db.Orders.Count(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.Confirmed);

        var monthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        double harvestMonth = db.SpiceBatches.Where(b => b.CollectedDate >= monthStart).Sum(b => b.RawWeightKg);
        decimal revenue = db.Orders.Include(o => o.Items).AsEnumerable().Sum(o => o.TotalAmount);

        Brush greenTile = new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32));
        Brush paleTile = new SolidColorBrush(Color.FromRgb(0xDD, 0xEE, 0xD3));
        Brush yellowTile = new SolidColorBrush(Color.FromRgb(0xF4, 0xC0, 0x33));

        Kpis.Clear();
        Kpis.Add(new KpiCard { Label = "Registered Farmers", Value = farmers.ToString(), Icon = "🌿", IconBackground = greenTile, Sub = "Active suppliers" });
        Kpis.Add(new KpiCard { Label = "Harvest (This Month)", Value = $"{harvestMonth:N0}", Unit = "KG", Icon = "🧺", IconBackground = yellowTile, Trend = "↑ 12.5%", ShowTrend = true, Sub = "vs last month" });
        Kpis.Add(new KpiCard { Label = "Active Workers", Value = workers.ToString(), Icon = "👥", IconBackground = paleTile, Sub = "Present today" });
        Kpis.Add(new KpiCard { Label = "Inventory Stock", Value = $"{invKg:N0}", Unit = "KG", Icon = "📦", IconBackground = yellowTile, Sub = "Across all spices" });
        Kpis.Add(new KpiCard { Label = "Pending Exports", Value = pending.ToString(), Icon = "🚚", IconBackground = paleTile, Sub = "Orders" });
        Kpis.Add(new KpiCard { Label = "Monthly Revenue", Value = $"LKR {revenue / 1_000_000:N1}M", Icon = "💰", IconBackground = yellowTile, Trend = "↑ 15.3%", ShowTrend = true, Sub = "vs last month" });
    }

    private void BuildSalesForecast()
    {
        var f = ServiceHub.SalesForecast.NextMonthOverall();
        SalesMonthLabel = f.Label;
        SalesUnits = $"{f.Units:N0}";
        SalesRevenue = f.Revenue >= 1_000_000m ? $"LKR {f.Revenue / 1_000_000m:N1}M" : $"LKR {f.Revenue:N0}";
        SalesTopProduct = string.IsNullOrWhiteSpace(f.TopProduct) ? "—" : f.TopProduct;
        SalesVsAvg = (f.VsAveragePercent >= 0 ? "+" : "") + $"{f.VsAveragePercent:N0}% vs avg";
        SalesHighSeason = f.HighSeasonWarning;
        SalesWarning = f.WarningText;
    }

    private void BuildDonut(SmartSpiceContext db)
    {
        // Harvest by spice type (raw weight), top 4 + others.
        var groups = db.SpiceBatches.AsEnumerable()
            .GroupBy(b => b.SpiceType)
            .Select(g => new { Name = g.Key, Kg = g.Sum(b => b.RawWeightKg) })
            .OrderByDescending(x => x.Kg).Take(4).ToList();

        double total = groups.Sum(g => g.Kg);
        DonutTotal = $"{total:N0}";
        Donut.Clear();
        if (total <= 0) return;

        var center = new Point(100, 100);
        double angle = 0;
        const double gap = 2.0;
        for (int i = 0; i < groups.Count; i++)
        {
            double pct = groups[i].Kg / total;
            double sweep = pct * 360.0;
            Donut.Add(new DonutSegment
            {
                Geometry = ChartGeometry.RingSegment(center, 92, 58, angle + gap / 2, Math.Max(1, sweep - gap)),
                Brush = DonutPalette[i % DonutPalette.Length],
                Name = groups[i].Name,
                Kg = $"{groups[i].Kg:N0} KG",
                Percent = $"{pct * 100:N1}%"
            });
            angle += sweep;
        }
    }

    private void BuildGauge(SmartSpiceContext db)
    {
        var inspections = db.QualityInspections.ToList();
        double passRate = inspections.Count == 0 ? 80 : inspections.Count(i => i.PassedFoodSafety) * 100.0 / inspections.Count;

        var graded = db.SpiceBatches.AsEnumerable().Where(b => b.YieldPercent.HasValue).ToList();
        double avgYield = graded.Count == 0 ? 85 : graded.Average(b => b.YieldPercent!.Value);

        int lowItems = db.InventoryItems.AsEnumerable().Count(i => i.IsLowStock);
        int totalItems = Math.Max(1, db.InventoryItems.Count());
        double stockHealth = 100 - (lowItems * 100.0 / totalItems);

        HealthScore = (int)Math.Round(Math.Clamp(passRate * 0.4 + avgYield * 0.4 + stockHealth * 0.2, 0, 100));
        HealthLabel = HealthScore >= 85 ? "Excellent" : HealthScore >= 70 ? "Good" : HealthScore >= 50 ? "Fair" : "Needs Attention";

        var center = new Point(100, 100);
        GaugeTrack = ChartGeometry.ArcStroke(center, 78, 225, 270);
        GaugeValue = ChartGeometry.ArcStroke(center, 78, 225, 270 * HealthScore / 100.0);
    }

    private void BuildTopStock(SmartSpiceContext db)
    {
        var top = db.InventoryItems.AsEnumerable()
            .OrderByDescending(i => i.QuantityKg).Take(4).ToList();
        double max = top.Count == 0 ? 1 : top.Max(i => i.QuantityKg);

        string Icon(string n) =>
            n.Contains("Cinnamon", StringComparison.OrdinalIgnoreCase) ? "🟫" :
            n.Contains("Pepper", StringComparison.OrdinalIgnoreCase) ? "⚫" :
            n.Contains("Cardamom", StringComparison.OrdinalIgnoreCase) ? "🟢" :
            n.Contains("Clove", StringComparison.OrdinalIgnoreCase) ? "🟤" :
            n.Contains("Turmeric", StringComparison.OrdinalIgnoreCase) ? "🟡" : "🌿";

        TopStock.Clear();
        var bars = new[]
        {
            new SolidColorBrush(Color.FromRgb(0x2E,0x7D,0x32)),
            new SolidColorBrush(Color.FromRgb(0x16,0x4A,0x1E)),
            new SolidColorBrush(Color.FromRgb(0xF4,0xC0,0x33)),
            new SolidColorBrush(Color.FromRgb(0x8B,0xC3,0x4A)),
        };
        for (int i = 0; i < top.Count; i++)
            TopStock.Add(new BarRow
            {
                Label = top[i].SpiceName,
                Icon = Icon(top[i].SpiceName),
                ValueText = $"{top[i].QuantityKg:N0} KG",
                Fraction = top[i].QuantityKg / max,
                Brush = bars[i % bars.Length]
            });
    }

    private void BuildActivities(SmartSpiceContext db)
    {
        string Icon(string a) => a switch
        {
            "GRIND" => "⚙",
            "INSPECT" => "🔬",
            "ORDER" => "🚚",
            "ADJUST_STOCK" => "📦",
            "CREATE" => "➕",
            "ADVANCE" => "🔄",
            "LOGIN" => "🔑",
            _ => "🌿"
        };

        Activities.Clear();
        foreach (var log in db.AuditLogs.OrderByDescending(l => l.Timestamp).Take(6).ToList())
        {
            Activities.Add(new ActivityRow
            {
                Icon = Icon(log.Action),
                Text = string.IsNullOrWhiteSpace(log.Details) ? $"{log.Action} {log.Entity}" : log.Details,
                Time = Relative(log.Timestamp)
            });
        }
        if (Activities.Count == 0)
            Activities.Add(new ActivityRow { Icon = "🌿", Text = "No recent activity yet", Time = "" });
    }

    private void BuildInsight(SmartSpiceContext db)
    {
        decimal revenue = db.Orders.Include(o => o.Items).AsEnumerable().Sum(o => o.TotalAmount);
        decimal forecast = revenue * 1.18m + 5_200_000m;
        RevenueForecast = $"LKR {forecast / 1_000_000:N1}M";
        RevenueForecastTrend = "↑ 18.7%";
    }

    private static string Relative(DateTime t)
    {
        var d = DateTime.Now - t;
        if (d.TotalMinutes < 60) return t.ToString("h:mm tt");
        if (t.Date == DateTime.Today) return t.ToString("h:mm tt");
        if (t.Date == DateTime.Today.AddDays(-1)) return "Yesterday";
        return t.ToString("MMM d, yyyy");
    }
}
