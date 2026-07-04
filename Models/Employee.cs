namespace SmartSpice.Models;

/// <summary>
/// A factory employee. Inherits identity from <see cref="Person"/> and adds
/// employment data plus the login account used for authentication.
/// </summary>
public class Employee : Person
{
    public string EmployeeCode { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.WarehouseStaff;
    public string Department { get; set; } = string.Empty;
    public DateTime HireDate { get; set; } = DateTime.Now;
    public bool IsActive { get; set; } = true;

    // --- Login credentials (ENCAPSULATION) ---
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime? LastLoginAt { get; set; }

    public override string RoleDescription => Role.ToString();
}
