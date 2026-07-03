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