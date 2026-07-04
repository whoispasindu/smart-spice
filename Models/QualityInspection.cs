namespace SmartSpice.Models;

public class QualityInspection
{
    public int Id { get; set; }
    
    public DateTime InspectedAt { get; set; } = DateTime.Now;
    public QualityGrade Grade { get; set; }
    public double MoisturePercent { get; set; }
    public double PurityPercent { get; set; }
    public bool PassedFoodSafety { get; set; }
    public bool ExportApproved { get; set; }
    public string Remarks { get; set; } = string.Empty;
    public string CertificateNo { get; set; } = string.Empty;

    public int BatchId { get; set; }
    public SpiceBatch? Batch{ get; set;}
    public int InspectorEmployeeId { get; set; }
    public Employee? Inspector { get; set;}
    
    public string ResultText => PassedFoodSafety ? "Passed" : "Rejected";
}