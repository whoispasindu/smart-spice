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

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source={DbPath}");

    protected override void OnModelCreating(ModelBuilder b)
    {
        // Money precision (honoured by SQL Server; SQLite stores as TEXT/REAL).
        b.Entity<InventoryItem>().Property(i => i.UnitPricePerKg).HasPrecision(18, 2);
        b.Entity<OrderItem>().Property(i => i.UnitPricePerKg).HasPrecision(18, 2);
        b.Entity<Buyer>().Property(x => x.CreditLimit).HasPrecision(18, 2);
        b.Entity<Buyer>().Property(x => x.OutstandingBalance).HasPrecision(18, 2);

        b.Entity<Employee>().HasIndex(e => e.Username).IsUnique();

        // A batch keeps its records; deleting a farmer must not cascade-wipe history.
        b.Entity<SpiceBatch>()
            .HasOne(s => s.Farmer).WithMany(f => f.Batches)
            .HasForeignKey(s => s.FarmerId).OnDelete(DeleteBehavior.Restrict);

        b.Entity<Order>()
            .HasOne(o => o.Buyer).WithMany(x => x.Orders)
            .HasForeignKey(o => o.BuyerId).OnDelete(DeleteBehavior.Restrict);
    }
}
    