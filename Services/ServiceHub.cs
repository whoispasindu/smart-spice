namespace SmartSpice.Services;

/// <summary>
/// Minimal composition root / service locator. Wires the shared service instances
/// once at startup so views and view-models can resolve their dependencies without
/// a heavyweight DI container.
/// </summary>
public static class ServiceHub
{
    public static AppSession Session { get; } = new();
    public static IAuditService Audit { get; } = new AuditService(Session);
    public static INotificationService Notifications { get; } = new NotificationService();
    public static IAuthService Auth { get; } = new AuthService(Session, Audit);
    public static IInventoryService Inventory { get; } = new InventoryService(Audit, Notifications);
    public static ISalesForecastService SalesForecast { get; } = new SalesForecastService();
}
