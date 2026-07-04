using SmartSpice.Data;
using SmartSpice.Models;

namespace SmartSpice.Services;

// ---------- result DTOs ----------
public class MonthForecast
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string Label => new DateTime(Year, Month, 1).ToString("MMM yyyy");
    public double Units { get; set; }
    public double Kg { get; set; }
    public decimal Revenue { get; set; }
    public bool IsHighSeason { get; set; }
    public double Fraction { get; set; }   // bar width vs the peak of the horizon
}

public class ProductForecast
{
    public string Product { get; set; } = string.Empty;
    public List<MonthForecast> Months { get; set; } = new();
    public bool HighSeasonWarning { get; set; }
    public string WarningText { get; set; } = string.Empty;
    public int ConfidencePercent { get; set; }
    public double TrendPctPerYear { get; set; }
    public double Avg12Units { get; set; }
}

public class ProductRank
{
    public string Product { get; set; } = string.Empty;
    public double Units { get; set; }
    public decimal Revenue { get; set; }
    public double Fraction { get; set; }
}

public class OverallForecast
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string Label => new DateTime(Year, Month, 1).ToString("MMMM yyyy");
    public double Units { get; set; }
    public decimal Revenue { get; set; }
    public string TopProduct { get; set; } = string.Empty;
    public bool HighSeasonWarning { get; set; }
    public string WarningText { get; set; } = string.Empty;
    public double VsAveragePercent { get; set; }
}

public interface ISalesForecastService
{
    IReadOnlyList<string> Products();
    ProductForecast Forecast(string product, int months = 3);
    IReadOnlyList<ProductRank> TopProducts(int months = 3);
    OverallForecast NextMonthOverall();
}

/// <summary>
/// Time-series sales forecaster. For each product it fits a linear <b>trend</b> plus
/// multiplicative <b>monthly seasonal indices</b> on the company's 3-year history
/// (classical decomposition), then projects the next months. The seasonal indices are
/// what let it flag an upcoming high-demand month so stock can be prepared.
/// </summary>
public class SalesForecastService : ISalesForecastService
{
    private const double HighSeasonThreshold = 1.12;   // 12% above the seasonal average

    public IReadOnlyList<string> Products()
    {
        using var db = new SmartSpiceContext();
        return db.SalesRecords.Select(s => s.Product).Distinct().OrderBy(p => p).ToList();
    }

    private static List<SalesRecord> Series(SmartSpiceContext db, string product) =>
        db.SalesRecords.Where(s => s.Product == product)
          .OrderBy(s => s.Year).ThenBy(s => s.Month).ToList();

    public ProductForecast Forecast(string product, int months = 3)
    {
        using var db = new SmartSpiceContext();
        var s = Series(db, product);
        var fc = new ProductForecast { Product = product };
        if (s.Count < 6) return fc;

        var units = s.Select(x => (double)x.Units).ToList();
        var mon = s.Select(x => x.Month).ToList();
        var (slope, intercept) = LinReg(units);
        var seasonal = SeasonalIndices(mon, units, slope, intercept);

        double kgPerUnit = units.Sum() > 0 ? s.Sum(x => x.Kg) / units.Sum() : 0;
        double revPerUnit = units.Sum() > 0 ? (double)s.Sum(x => x.Revenue) / units.Sum() : 0;

        // confidence from how well the model fits history
        double mean = units.Average();
        double sse = 0;
        for (int i = 0; i < units.Count; i++)
        {
            double fit = Math.Max(0, (slope * i + intercept) * seasonal[mon[i]]);
            sse += (units[i] - fit) * (units[i] - fit);
        }
        double rmse = Math.Sqrt(sse / units.Count);
        fc.ConfidencePercent = (int)Math.Round(Math.Clamp(100 - rmse / Math.Max(1, mean) * 100, 55, 97));
        fc.Avg12Units = units.TakeLast(12).Average();
        double level0 = intercept, levelNow = slope * (units.Count - 1) + intercept;
        fc.TrendPctPerYear = level0 > 0 ? Math.Round((slope * 12) / level0 * 100, 1) : 0;

        // Forecast the calendar months immediately ahead of *now* (the model
        // extrapolates the trend and applies each month's learned seasonal factor).
        var firstMonth = s[0].PeriodStart;
        var now = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        double peak = 0;
        for (int k = 1; k <= months; k++)
        {
            var d = now.AddMonths(k);
            int t = (d.Year - firstMonth.Year) * 12 + (d.Month - firstMonth.Month);
            double idx = seasonal[d.Month];
            double u = Math.Max(0, (slope * t + intercept) * idx);
            var m = new MonthForecast
            {
                Year = d.Year,
                Month = d.Month,
                Units = Math.Round(u),
                Kg = Math.Round(u * kgPerUnit, 1),
                Revenue = (decimal)Math.Round(u * revPerUnit, 0),
                IsHighSeason = idx >= HighSeasonThreshold
            };
            fc.Months.Add(m);
            peak = Math.Max(peak, u);
        }
        foreach (var m in fc.Months) m.Fraction = peak > 0 ? m.Units / peak : 0;

        // warn if any of the next months is a high-demand month
        var hot = fc.Months.FirstOrDefault(m => m.IsHighSeason);
        if (hot != null)
        {
            double pct = (seasonal[hot.Month] - 1) * 100;
            fc.HighSeasonWarning = true;
            fc.WarningText = $"High demand expected in {hot.Label} — about {pct:N0}% above average. " +
                             $"Increase {product} stock now to avoid a shortage.";
        }
        return fc;
    }

    public IReadOnlyList<ProductRank> TopProducts(int months = 3)
    {
        var ranks = new List<ProductRank>();
        foreach (var p in Products())
        {
            var f = Forecast(p, months);
            ranks.Add(new ProductRank
            {
                Product = p,
                Units = f.Months.Sum(m => m.Units),
                Revenue = f.Months.Sum(m => m.Revenue)
            });
        }
        double max = ranks.Count == 0 ? 1 : Math.Max(1, ranks.Max(r => r.Units));
        foreach (var r in ranks) r.Fraction = r.Units / max;
        return ranks.OrderByDescending(r => r.Units).ToList();
    }

    public OverallForecast NextMonthOverall()
    {
        var result = new OverallForecast();
        double total = 0; decimal rev = 0; string top = ""; double topU = 0;
        double avg12Sum = 0;
        int year = 0, month = 0;

        foreach (var p in Products())
        {
            var f = Forecast(p, 1);
            if (f.Months.Count == 0) continue;
            var m = f.Months[0];
            year = m.Year; month = m.Month;
            total += m.Units; rev += m.Revenue; avg12Sum += f.Avg12Units;
            if (m.Units > topU) { topU = m.Units; top = p; }
        }
        result.Year = year; result.Month = month;
        result.Units = Math.Round(total);
        result.Revenue = rev;
        result.TopProduct = top;
        result.VsAveragePercent = avg12Sum > 0 ? Math.Round((total / avg12Sum - 1) * 100, 0) : 0;
        if (result.VsAveragePercent >= HighSeasonThreshold * 100 - 100) // >= ~12%
        {
            result.HighSeasonWarning = true;
            result.WarningText = $"{result.Label} is a high-sales month (about {result.VsAveragePercent:N0}% above average). " +
                                 "Prepare extra stock.";
        }
        return result;
    }

    // ---------- model maths ----------
    private static (double slope, double intercept) LinReg(IReadOnlyList<double> y)
    {
        int n = y.Count;
        double sx = 0, sy = 0, sxx = 0, sxy = 0;
        for (int i = 0; i < n; i++) { sx += i; sy += y[i]; sxx += i * (double)i; sxy += i * y[i]; }
        double denom = n * sxx - sx * sx;
        double slope = denom == 0 ? 0 : (n * sxy - sx * sy) / denom;
        double intercept = (sy - slope * sx) / n;
        return (slope, intercept);
    }

    /// <summary>Average actual/trend ratio per calendar month, normalised to mean 1.</summary>
    private static double[] SeasonalIndices(IReadOnlyList<int> months, IReadOnlyList<double> y, double slope, double intercept)
    {
        var sum = new double[13];
        var count = new int[13];
        for (int i = 0; i < y.Count; i++)
        {
            double trend = slope * i + intercept;
            if (trend <= 0) continue;
            sum[months[i]] += y[i] / trend;
            count[months[i]]++;
        }
        var idx = new double[13];
        double tot = 0;
        for (int m = 1; m <= 12; m++) { idx[m] = count[m] > 0 ? sum[m] / count[m] : 1.0; tot += idx[m]; }
        double mean = tot / 12.0;
        if (mean > 0) for (int m = 1; m <= 12; m++) idx[m] /= mean;
        return idx;
    }
}