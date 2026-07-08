namespace SmartSpice.Services;


public static class ServiceHub
{
    public static AppSession Session { get; } = new();
    public static IAuditService Audit { get; } = new AuditService(Session);
    public static INotificationService Notifications { get; } = new NotificationService();
    public static IAuthService Auth { get; } = new AuthService(Session, Audit);
    public static IInventoryService Inventory { get; } = new InventoryService(Audit, Notifications);
    public static ISalesForecastService SalesForecast { get; } = new SalesForecastService();
}
