using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using Microsoft.EntityFrameworkCore;
using SmartSpice.Data;
using SmartSpice.Helpers;
using SmartSpice.Models;
using SmartSpice.Services;

namespace SmartSpice.ViewModels;


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

    // AI sales-forecast feature 
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
        percent >= 90 ? Color.FromRgb(0xD1, 0x43, 0x43)  
      : percent >= 75 ? Color.FromRgb(0xEF, 0x8A, 0x1F)   
      : Color.FromRgb(0x3E, 0x8E, 0x2E));                 

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
     