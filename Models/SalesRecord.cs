namespace SmartSpice.Models;

/// <summary>
/// One month of aggregated sales for a product — the historical data the AI
/// sales-prediction model is trained on (imported from the company's 3-year records).
/// </summary>
public class SalesRecord
{
    public int Id { get; set; }
    public string Product { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Month { get; set; }          // 1-12
    public int Units { get; set; }          // packs sold
    public double Kg { get; set; }          // total weight sold
    public decimal Revenue { get; set; }    // LKR

    public DateTime PeriodStart => new(Year, Month, 1);
    public string PeriodLabel => PeriodStart.ToString("MMM yyyy");
}
