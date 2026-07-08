using System.Windows.Controls;

namespace SmartSpice.Models;


public class SpiceBatch
{
    public int Id { get; set; }
    public string BatchCode { get; set; } = string.Empty;
    public string SpiceType { get; set; } = string.Empty;
    public BatchStatus Status { get; set; } = BatchStatus.Collected;

    public double RawWeightKg { get; set; }
    public double? ProcessedWeightKg { get; set; }
    public double MoisturePercent { get; set; }

    public double DryingHours { get; set; } = 24;

    public DateTime CollectedDate { get; set; } = DateTime.Now;
    public DateTime? CompletedDate { get; set; }

    public int FarmerId { get; set; }
    public Farmer? Farmer { get; set; }

    public int? WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }

    public int? AssignedEmployeeId { get; set; }
    public Employee? AssignedEmployee { get; set; }

    public List<ProcessingRecord> ProcessingRecords { get; set; } = new();
    public List<QualityInspection> Inspections { get; set; } = new();

    public double? YieldLossPercent =>
        ProcessedWeightKg.HasValue && RawWeightKg > 0
            ? Math.Round((RawWeightKg - ProcessedWeightKg.Value) / RawWeightKg * 100, 2)
            : null;

    public double? YieldPercent =>
        YieldLossPercent.HasValue ? Math.Round(100 - YieldLossPercent.Value, 2) : null;
}
