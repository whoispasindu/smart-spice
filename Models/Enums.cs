namespace SmartSpice.Models;

/// <summary>
/// Role-based access control levels for the system.
/// </summary>
public enum UserRole
{
    Administrator,
    Manager,
    WarehouseStaff,
    QualityInspector,
    SalesOfficer
}

/// <summary>
/// Distinguishes raw agricultural input from finished processed powder.
/// </summary>
public enum InventoryCategory
{
    RawMaterial,
    ProcessedPowder,
    Packaging
}

/// <summary>
/// The lifecycle stage of a spice batch as it moves through the pipeline.
/// Mirrors the business workflow described in the proposal.
/// </summary>
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

/// <summary>
/// Quality grade assigned during inspection or predicted by the AI model.
/// </summary>
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
