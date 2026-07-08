namespace SmartSpice.Models;

public class ProcessingRecord
{
    public int Id { get; set; }
    public BatchStatus Stage { get; set; }
    public double WeightBeforeKg { get; set; }
    public double WeightAfterKg { get; set; }
    public string Method { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime PerformedAt { get; set; } = DateTime.Now;

    public int BatchId { get; set; }
    public SpiceBatch? Batch { get; set; }

    public int OperatorEmployeeId { get; set; }
    public Employee? Operator { get; set; }

    public double WeightLossKg => Math.Max(0, WeightBeforeKg - WeightAfterKg);

    public double LossPercent =>
        WeightBeforeKg > 0 ? Math.Round(WeightLossKg / WeightBeforeKg * 100, 2) : 0;
}
