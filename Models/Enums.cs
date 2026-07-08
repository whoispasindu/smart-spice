namespace SmartSpice.Models;

public enum UserRole
{
    Administrator,
    Manager,
    WarehouseStaff,
    QualityInspector,
    SalesOfficer
}


public enum InventoryCategory
{
    RawMaterial,
    ProcessedPowder,
    Packaging
}

public enum BatchStatus
{
    Collected,
    Received,
    Cleaning,
    Drying,
    Grinding,
    Packaging,
    Stored,
    Dispatched
}

public enum QualityGrade
{
    A,
    B,
    C,
    D,
    Rejected
}

public enum OrderStatus
{
    Pending,
    Confirmed,
    Packed,
    Dispatched,
    Delivered,
    Cancelled
}

public enum NotificationType
{
    LowStock,
    Expiry,
    QualityAlert,
    Shipment,
    System
}

public enum NotificationSeverity
{
    Info,
    Warning,
    Critical
}
