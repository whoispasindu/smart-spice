using System.Windows.Controls;

namespace SmartSpice.Models;

public class Buyer : Person
{
    public string CompanyName { get; set; } = string.Empty;
    public string Country { get; set; } = "Sri Lanka";
    public bool IsExport { get; set; }
    public decimal CreditLimit { get; set; }
    public decimal OutstandingBalance { get; set; }

    public override string RoleDescription => IsExport ? "Export Buyer" : "Local Buyer";

    public List<Order> Orders { get; set; } = new();
}