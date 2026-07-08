namespace SmartSpice.Models;


public class Farmer : Person
{
    public string FarmName { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public double FarmSizeAcres { get; set; }
    public string PrimaryCrops { get; set; } = string.Empty;
    public string BankAccount { get; set; } = string.Empty;
    public bool IsCertifiedOrganic { get; set; }

    public double ReliabilityScore { get; set; } = 80;

    public override string RoleDescription => "Farmer";

    public List<SpiceBatch> Batches { get; set; } = new();
}
