namespace SmartSpice.Models;

/// <summary>
/// A traceable lot of spice moving through the processing pipeline, from farm
/// collection to dispatch. The unifying entity behind full supply-chain traceability.
/// </summary>
public class SpiceBatch
{
    public int Id { get; set; }
    public string BatchCode { get; set; } = string.Empty;
    public string SpiceType { get; set; } = string.Empty;
    public BatchStatus Status { get; set; } = BatchStatus.Collected;

    public double RawWeightKg { get; set; }
    public double? ProcessedWeightKg { get; set; }
    public double MoisturePercent { get; set; }

    /// <summary>Hours the batch was dried — an input to the AI yield prediction.</summary>
    public double DryingHours { get; set; } = 24;

    public DateTime CollectedDate { get; set; } = DateTime.Now;
    public DateTime? CompletedDate { get; set; }

    public int FarmerId { get; set; }
    public Farmer? Farmer { get; set; }

    public int? WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }

    /// <summary>The employee accountable for this batch (labour accountability).</summary>
    public int? AssignedEmployeeId { get; set; }
    public Employee? AssignedEmployee { get; set; }

    public List<ProcessingRecord> ProcessingRecords { get; set; } = new();
    public List<QualityInspection> Inspections { get; set; } = new();

    /// <summary>
    /// Yield loss = weight dropped to dust + moisture during grinding, as a percentage.
    /// </summary>
    public double? YieldLossPercent =>
        ProcessedWeightKg.HasValue && RawWeightKg > 0
            ? Math.Round((RawWeightKg - ProcessedWeightKg.Value) / RawWeightKg * 100, 2)
            : null;

    public double? YieldPercent =>
        YieldLossPercent.HasValue ? Math.Round(100 - YieldLossPercent.Value, 2) : null;
}
