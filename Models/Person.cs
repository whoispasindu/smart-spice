namespace SmartSpice.Models;

/// <summary>
/// Abstract base for every human entity in the system.
/// Demonstrates ABSTRACTION and INHERITANCE: <see cref="Employee"/>, <see cref="Buyer"/>
/// and <see cref="Farmer"/> all extend this base, reusing the common identity fields.
/// </summary>
public abstract class Person
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// POLYMORPHISM: each subclass returns its own descriptive role label.
    /// Overridden by Employee, Buyer and Farmer.
    /// </summary>
    public abstract string RoleDescription { get; }

    /// <summary>Used in lists / combo boxes across the UI.</summary>
    public string DisplayName => $"{FullName} ({RoleDescription})";
}
