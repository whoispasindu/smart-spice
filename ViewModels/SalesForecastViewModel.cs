using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SmartSpice.Services;

namespace SmartSpice.ViewModels;

public partial class SalesForecastViewModel : ViewModelBase
{
    private readonly ISalesForecastService _svc = ServiceHub.SalesForecast;

    public ObservableCollection<string> Products { get; } = new();
    public ObservableCollection<MonthForecast> Forecast { get; } = new();
    public ObservableCollection<ProductRank> TopProducts { get; } = new();

    [ObservableProperty] private string? _selectedProduct;
    [ObservableProperty] private int _confidence;
    [ObservableProperty] private string _trendText = "—";
    [ObservableProperty] private bool _hasWarning;
    [ObservableProperty] private string _warningText = string.Empty;
    [ObservableProperty] private string _horizonTotal = "—";

    public SalesForecastViewModel() => Title = "AI Sales Prediction";

    public override void Load()
    {
        Products.Clear();
        foreach (var p in _svc.Products()) Products.Add(p);

        TopProducts.Clear();
        foreach (var r in _svc.TopProducts(3)) TopProducts.Add(r);

        SelectedProduct = Products.FirstOrDefault();   // triggers Run()
    }

    partial void OnSelectedProductChanged(string? value) => Run();

    [RelayCommand]
    private void Run()
    {
        Forecast.Clear();
        if (string.IsNullOrWhiteSpace(SelectedProduct)) return;

        var f = _svc.Forecast(SelectedProduct, 3);
        foreach (var m in f.Months) Forecast.Add(m);

        Confidence = f.ConfidencePercent;
        TrendText = (f.TrendPctPerYear >= 0 ? "+" : "") + $"{f.TrendPctPerYear:N1}% / year";
        HasWarning = f.HighSeasonWarning;
        WarningText = f.WarningText;
        HorizonTotal = $"{f.Months.Sum(m => m.Units):N0} units  •  LKR {f.Months.Sum(m => m.Revenue):N0}";
    }
}
