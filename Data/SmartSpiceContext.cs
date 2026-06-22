using Microsoft.EntityFrameworkCore;
using SmartSpice.Models;
using System.IO;
using System.Reflection.Emit;
using System.Windows.Controls;

namespace SmartSpice.Data;

/// <summary>
/// EF Core context. Uses a local SQLite file so the app runs in Visual Studio with
/// zero server setup, while keeping a normalized, SQL-Server-compatible schema.
/// </summary>
public class SmartSpiceContext : DbContext
{
    // TODO: Review usage of Employees DbSet.
    // TODO: Review usage of Farmers DbSet.
    // TODO: Review usage of Buyers DbSet.
    // TODO: Review usage of Warehouses DbSet.
    // TODO: Review usage of InventoryItems DbSet.
    // TODO: Review usage of SpiceBatches DbSet.
    // TODO: Review usage of ProcessingRecords DbSet.
    // TODO: Review usage of QualityInspections DbSet.
    // TODO: Review usage of Orders DbSet.
    // TODO: Review usage of OrderItems DbSet.
    // TODO: Review usage of Notifications.
    // TODO: Review usage of AuditLogs DbSet.
    // TODO: Review usage of SalesRecords DbSet.

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
    