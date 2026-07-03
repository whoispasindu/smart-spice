namespace SmartSpice.Models;

/// <summary>
/// An immutable activity record creating the audit trail that links every
/// critical action to a specific employee for total accountability.
/// </summary>
public class AuditLog
{
    public int Id { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Username { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}
