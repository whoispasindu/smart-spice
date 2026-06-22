using SmartSpice.Models;
using System.IO;

namespace SmartSpice.Data;

/// <summary>
/// Creates the database (if missing) and populates realistic demo data so every
/// screen is meaningful the first time the app is launched.
/// </summary>
public static class DbSeeder
{
    public static void EnsureSeeded(SmartSpiceContext db)
    {
        // Schema guard: if an existing database predates a newer column/table
        // (e.g. DryingHours, SalesRecords), drop it so it is rebuilt — self-heals
        // without manual EF migrations.

        db.Database.EnsureCreated();

        // ---------- Employees / login accounts ----------

        // ---------- Warehouses ----------
       
        // ---------- Farmers ----------

        // ---------- Buyers ----------


        // ---------- Spice batches (only the catalogue products) ----------
        var rnd = new Random(42);
        var defs = new (string product, BatchStatus status)[]
        {
            ("Dried Red Chillies",      BatchStatus.Stored),
            ("Black Pepper Corns",      BatchStatus.Stored),
            ("Fresh Turmeric Rhizomes", BatchStatus.Stored),
            ("Cardamom",                BatchStatus.Stored),
            ("Mustard Seeds",           BatchStatus.Stored),
            ("Cinnamon",                BatchStatus.Packaging),
            ("Cloves",                  BatchStatus.Grinding),
            ("Coriander Seeds",         BatchStatus.Drying),
            ("Cumin Seeds",             BatchStatus.Cleaning),
            ("Curry Leaves",            BatchStatus.Received),
            ("Fennel Seeds",            BatchStatus.Collected),
            ("Fenugreek Seeds",         BatchStatus.Grinding),
        };

        // accumulate the inventory each batch produces (raw if in-process, powder if stored)
        
        // ---------- Inventory (derived from batches + packaging) ----------
       
        // ---------- Orders ----------
        
        // ---------- Notifications (recompute from seeded stock) ----------
       

        // Import 3 years of monthly sales history (trains the AI sales predictor).
     
    }

    /// <summary>
    /// Probes a column added in a later version; if the query fails, the database
    /// predates the change and must be rebuilt.
    /// </summary>
    /// <summary>
    /// True when an existing database is missing newer columns/tables and must be
    /// rebuilt. Uses its own short-lived context so the main one stays clean.
    /// </summary>
   

    /// <summary>Imports the company's monthly sales history from the bundled CSV.</summary>
    
    /// <summary>Rough per-kg price by product/powder name.</summary>
    private static decimal PriceOf(string name) => name switch
    {
        var n when n.Contains("Cardamom") => 7000m,
        var n when n.Contains("Clove") => 2600m,
        var n when n.Contains("Pepper") => 2200m,
        var n when n.Contains("Cinnamon") => 1600m,
        var n when n.Contains("Cumin") => 1400m,
        var n when n.Contains("Fennel") => 1300m,
        var n when n.Contains("Turmeric") => 1250m,
        var n when n.Contains("Chilli") => 1100m,
        var n when n.Contains("Curry") => 1000m,
        var n when n.Contains("Coriander") => 900m,
        var n when n.Contains("Fenugreek") => 800m,
        var n when n.Contains("Mustard") => 700m,
        var n when n.Contains("Pandan") => 600m,
        _ => 1000m
    };
}
