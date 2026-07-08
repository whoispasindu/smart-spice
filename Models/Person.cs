namespace SmartSpice.Models;


public abstract class Person
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public abstract string RoleDescription { get; }

    public string DisplayName => $"{FullName} ({RoleDescription})";
}
