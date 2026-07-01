using Microsoft.EntityFrameworkCore;
using SmartSpice.Models;
using System.IO;
using System.Windows.Controls;

namespace SmartSpice.Data;

/// <summary>
/// EF Core context. Uses a local SQLite file so the app runs in Visual Studio with
/// zero server setup, while keeping a normalized, SQL-Server-compatible schema.
/// </summary>
public class SmartSpiceContext : DbContext
{
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Farmer> Farmers => Set<Farmer>();
    public DbSet<Buyer> Buyers => Set<Buyer>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<SpiceBatch> SpiceBatches => Set<SpiceBatch>();
    public DbSet<ProcessingRecord> ProcessingRecords => Set<ProcessingRecord>();
    public DbSet<QualityInspection> QualityInspections => Set<QualityInspection>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SalesRecord> SalesRecords => Set<SalesRecord>();

    /// <summary>Absolute path to the SQLite database file in the app's data folder.</summary>
    public static string DbPath
    {
        get
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SmartSpice");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "smartspice.db");
        }
    }

    protected override void OnModelCreating(ModelBuilder b)
    {
        // TODO: Review precision for InventoryItem.UnitPricePerKg and update if needed.
        // Money precision (honoured by SQL Server; SQLite stores as TEXT/REAL).

        // TODO: Review precision for OrderItem.UnitPricePerKg and update if needed.

        // TODO: Review precision for Buyer.CreditLimit and update if needed.

        // TODO: Review precision for Buyer.OutstandingBalance and update if needed.

        // TODO: Confirm Employee.Username should be indexed and unique.

        // TODO: Confirm delete behaviour: keep SpiceBatch records when a Farmer is deleted.
        // A batch keeps its records; deleting a farmer must not cascade-wipe history.

        // TODO: Confirm delete behaviour: restrict deleting Buyer when Orders exist.
    }
}
    