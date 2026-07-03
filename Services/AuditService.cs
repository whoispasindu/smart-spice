using Microsoft.EntityFrameworkCore;
using SmartSpice.Data;
using SmartSpice.Models;

namespace SmartSpice.Services;

public class AuditService : IAuditService
{
    private readonly AppSession _session;

    public AuditService(AppSession session) => _session = session;

    public void Log(string action, string entity, string details)
    {
        try
        {
            using var db = new SmartSpiceContext();
            db.AuditLogs.Add(new AuditLog
            {
                Username = _session.CurrentUser?.Username ?? "system",
                Action = action,
                Entity = entity,
                Details = details
            });
            db.SaveChanges();
        }
        catch
        {
            // Auditing must never crash the user's action.
        }
    }

    public IReadOnlyList<AuditLog> GetRecent(int count = 200)
    {
        using var db = new SmartSpiceContext();
        return db.AuditLogs
            .OrderByDescending(a => a.Timestamp)
            .Take(count)
            .ToList();
    }
}
