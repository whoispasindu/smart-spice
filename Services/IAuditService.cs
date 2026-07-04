using SmartSpice.Models;

namespace SmartSpice.Services;

/// <summary>
/// ABSTRACTION via interface — the audit trail contract used across the app.
/// </summary>
public interface IAuditService
{
    void Log(string action, string entity, string details);
    IReadOnlyList<AuditLog> GetRecent(int count = 200);
}

