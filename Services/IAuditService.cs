using SmartSpice.Models;

namespace SmartSpice.Services;


public interface IAuditService
{
    void Log(string action, string entity, string details);
    IReadOnlyList<AuditLog> GetRecent(int count = 200);
}

