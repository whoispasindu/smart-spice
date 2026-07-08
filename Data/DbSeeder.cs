using SmartSpice.Helpers;
using SmartSpice.Models;
using System.IO;

namespace SmartSpice.Data;

public static class DbSeeder
{
    public static void EnsureSeeded(SmartSpiceContext db)
    {
        // Schema guard: if an existing database predates a newer column/table
        // (e.g. DryingHours, SalesRecords), drop it so it is rebuilt — self-heals
        // without manual EF migrations.
        if (DatabaseIsStale())
        {
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            db.Database.EnsureDeleted();
        }

        db.Database.EnsureCreated();

        if (db.Employees.Any()) return; // already seeded

        // ---------- Employees / login accounts ----------
        var employees = new List<Employee>
        {
            NewEmp("Admin User", "admin", "admin123", UserRole.Administrator, "EMP-001", "Management"),
            NewEmp("Saman Perera", "manager", "manager123", UserRole.Manager, "EMP-002", "Operations"),
            NewEmp("Nimal Silva", "warehouse", "store123", UserRole.WarehouseStaff, "EMP-003", "Warehouse"),
            NewEmp("Kamala Fernando", "quality", "quality123", UserRole.QualityInspector, "EMP-004", "Quality Control"),
            NewEmp("Ruwan Jayasuriya", "sales", "sales123", UserRole.SalesOfficer, "EMP-005", "Sales"),
        };
        db.Employees.AddRange(employees);

        // ---------- Warehouses ----------
        var whRaw = new Warehouse { Name = "Raw Materials Store", Location = "Matale - Block A", CapacityKg = 15000 };
        var whPowder = new Warehouse { Name = "Processed Powder Store", Location = "Matale - Block B", CapacityKg = 8000 };
        var whExport = new Warehouse { Name = "Export Holding", Location = "Colombo Port", CapacityKg = 5000 };
        db.Warehouses.AddRange(whRaw, whPowder, whExport);

        // ---------- Farmers ----------
        var farmers = new List<Farmer>
        {
            new() { FullName = "Bandara Wickramasinghe", FarmName = "Hill Country Spices", Region = "Matale", FarmSizeAcres = 12, PrimaryCrops = "Cinnamon, Pepper", Phone = "0712345678", IsCertifiedOrganic = true, ReliabilityScore = 92 },
            new() { FullName = "Anura Dissanayake", FarmName = "Green Valley Estate", Region = "Kandy", FarmSizeAcres = 8, PrimaryCrops = "Cardamom, Cloves", Phone = "0723456789", ReliabilityScore = 85 },
            new() { FullName = "Priyanka Gunawardena", FarmName = "Sunrise Plantation", Region = "Kegalle", FarmSizeAcres = 20, PrimaryCrops = "Turmeric, Cinnamon", Phone = "0734567890", IsCertifiedOrganic = true, ReliabilityScore = 88 },
            new() { FullName = "Sunil Rathnayake", FarmName = "Spice Garden Co-op", Region = "Galle", FarmSizeAcres = 15, PrimaryCrops = "Pepper, Cloves", Phone = "0745678901", ReliabilityScore = 79 },
        };
        db.Farmers.AddRange(farmers);

        // ---------- Buyers ----------
        var buyers = new List<Buyer>
        {
            new() { FullName = "Lanka Spice Exports (Pvt) Ltd", CompanyName = "Lanka Spice Exports", Country = "Germany", IsExport = true, CreditLimit = 5_000_000, Email = "orders@lankaspice.lk", Phone = "0112556677" },
            new() { FullName = "Cargills Food City", CompanyName = "Cargills Ceylon", Country = "Sri Lanka", IsExport = false, CreditLimit = 2_000_000, Email = "procurement@cargills.lk", Phone = "0114787878" },
            new() { FullName = "Dubai Trading FZE", CompanyName = "Dubai Trading", Country = "UAE", IsExport = true, CreditLimit = 8_000_000, Email = "buy@dubaitrading.ae", Phone = "0097145551234" },
        };
        db.Buyers.AddRange(buyers);

        db.SaveChanges(); // assign IDs

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
        var invAcc = new Dictionary<(string name, InventoryCategory cat, int wh), double>();
        void AddInv(string name, InventoryCategory cat, int wh, double kg)
        {
            var key = (name, cat, wh);
            invAcc[key] = invAcc.GetValueOrDefault(key) + kg;
        }

        var batches = new List<SpiceBatch>();
        for (int i = 0; i < defs.Length; i++)
        {
            var (product, status) = defs[i];
            double raw = 200 + rnd.Next(0, 700);
            var batch = new SpiceBatch
            {
                BatchCode = $"BATCH-2026-{i + 1:000}",
                SpiceType = product,
                FarmerId = farmers[rnd.Next(farmers.Count)].Id,
                RawWeightKg = raw,
                MoisturePercent = 8 + rnd.NextDouble() * 12,
                DryingHours = 18 + rnd.Next(0, 14),
                Status = status,
                CollectedDate = DateTime.Now.AddDays(-rnd.Next(1, 60)),
                AssignedEmployeeId = employees[2].Id,
                WarehouseId = whRaw.Id
            };

            if (status >= BatchStatus.Grinding)
            {
                double loss = 0.06 + rnd.NextDouble() * 0.12;
                batch.ProcessedWeightKg = Math.Round(raw * (1 - loss), 1);
                batch.ProcessingRecords.Add(new ProcessingRecord
                {
                    Stage = BatchStatus.Grinding,
                    Method = "Mechanical Mill",
                    WeightBeforeKg = raw,
                    WeightAfterKg = batch.ProcessedWeightKg.Value,
                    OperatorEmployeeId = employees[2].Id,
                    PerformedAt = batch.CollectedDate.AddDays(3),
                    Notes = "Routine grind"
                });
                batch.Inspections.Add(new QualityInspection
                {
                    Grade = (QualityGrade)rnd.Next(0, 3),
                    MoisturePercent = batch.MoisturePercent,
                    PurityPercent = 90 + rnd.NextDouble() * 9,
                    PassedFoodSafety = true,
                    ExportApproved = rnd.Next(0, 2) == 1,
                    InspectorEmployeeId = employees[3].Id,
                    CertificateNo = $"QC-{i + 1:000}",
                    Remarks = "Within spec",
                    InspectedAt = batch.CollectedDate.AddDays(4)
                });
            }

            // A stored batch has become its finished powder; otherwise it's still raw stock.
            if (status >= BatchStatus.Stored)
            {
                batch.CompletedDate = batch.CollectedDate.AddDays(5);
                AddInv(SpiceCatalog.PowderFor(product), InventoryCategory.ProcessedPowder, whPowder.Id,
                       batch.ProcessedWeightKg ?? Math.Round(raw * 0.9, 1));
            }
            else
            {
                AddInv(product, InventoryCategory.RawMaterial, whRaw.Id, raw);
            }
            batches.Add(batch);
        }
        db.SpiceBatches.AddRange(batches);

        // ---------- Inventory (derived from batches + packaging) ----------
        foreach (var ((name, cat, wh), kg) in invAcc)
            db.InventoryItems.Add(new InventoryItem
            {
                SpiceName = name,
                Category = cat,
                WarehouseId = wh,
                QuantityKg = Math.Round(kg, 1),
                ReorderLevelKg = Math.Round(kg * 0.2, 0),
                UnitPricePerKg = PriceOf(name),
                ExpiryDate = cat == InventoryCategory.ProcessedPowder ? DateTime.Now.AddMonths(8) : null
            });
        db.InventoryItems.Add(new InventoryItem
        {
            SpiceName = "Vacuum Pouches 500g",
            Category = InventoryCategory.Packaging,
            WarehouseId = whPowder.Id,
            QuantityKg = 1200,
            ReorderLevelKg = 1500,
            UnitPricePerKg = 18m  // LOW
        });

        // ---------- Orders ----------
        db.Orders.AddRange(
            new Order
            {
                InvoiceNo = "INV-2026-001",
                BuyerId = buyers[0].Id,
                Status = OrderStatus.Confirmed,
                HandledByEmployeeId = employees[4].Id,
                OrderedDate = DateTime.Now.AddDays(-5),
                Items =
                {
                    new OrderItem { SpiceName = "Chilli Powder", QuantityKg = 80, UnitPricePerKg = 1100 },
                    new OrderItem { SpiceName = "Pepper Powder", QuantityKg = 40, UnitPricePerKg = 2200 },
                }
            },
            new Order
            {
                InvoiceNo = "INV-2026-002",
                BuyerId = buyers[1].Id,
                Status = OrderStatus.Pending,
                HandledByEmployeeId = employees[4].Id,
                OrderedDate = DateTime.Now.AddDays(-2),
                Items = { new OrderItem { SpiceName = "Turmeric Powder", QuantityKg = 60, UnitPricePerKg = 1250 } }
            },
            new Order
            {
                InvoiceNo = "INV-2026-003",
                BuyerId = buyers[2].Id,
                Status = OrderStatus.Dispatched,
                HandledByEmployeeId = employees[4].Id,
                OrderedDate = DateTime.Now.AddDays(-12),
                DispatchedDate = DateTime.Now.AddDays(-9),
                Items = { new OrderItem { SpiceName = "Cardamom Powder", QuantityKg = 50, UnitPricePerKg = 7000 } }
            }
        );

        db.SaveChanges();

        // ---------- Notifications (recompute from seeded stock) ----------
        foreach (var item in db.InventoryItems.Where(i => i.QuantityKg <= i.ReorderLevelKg))
        {
            db.Notifications.Add(new Notification
            {
                Type = NotificationType.LowStock,
                Severity = NotificationSeverity.Warning,
                Title = $"Low stock: {item.SpiceName}",
                Message = $"{item.SpiceName} is at {item.QuantityKg:N0} kg (reorder at {item.ReorderLevelKg:N0} kg)."
            });
        }
        db.Notifications.Add(new Notification
        {
            Type = NotificationType.System,
            Severity = NotificationSeverity.Info,
            Title = "Welcome to SmartSpice",
            Message = "Demo data loaded. Explore the dashboard to begin."
        });

        db.AuditLogs.Add(new AuditLog
        {
            Username = "system",
            Action = "SEED",
            Entity = "Database",
            Details = "Initial demo dataset created."
        });

        db.SaveChanges();

        // Import 3 years of monthly sales history (trains the AI sales predictor).
        SeedSalesHistory(db);
    }

    private static bool DatabaseIsStale()
    {
        using var probe = new SmartSpiceContext();
        if (!probe.Database.CanConnect()) return false;   
        try
        {
            _ = probe.SpiceBatches.Select(b => b.DryingHours).FirstOrDefault();
            _ = probe.SalesRecords.Select(s => s.Id).FirstOrDefault();
            return false;
        }
        catch
        {
            return true;
        }
    }

   
    private static void SeedSalesHistory(SmartSpiceContext db)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "Data", "sales_monthly.csv");
        if (!File.Exists(path)) return;

        var records = new List<SalesRecord>();
        foreach (var line in File.ReadLines(path).Skip(1))   // skip header
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var c = line.Split(',');
            if (c.Length < 6) continue;
            try
            {
                records.Add(new SalesRecord
                {
                    Product = c[0].Trim(),
                    Year = int.Parse(c[1]),
                    Month = int.Parse(c[2]),
                    Units = int.Parse(c[3]),
                    Kg = double.Parse(c[4], System.Globalization.CultureInfo.InvariantCulture),
                    Revenue = decimal.Parse(c[5], System.Globalization.CultureInfo.InvariantCulture)
                });
            }
            catch { /* skip malformed row */ }
        }
        db.SalesRecords.AddRange(records);
        db.SaveChanges();
    }

    private static Employee NewEmp(string name, string user, string pwd, UserRole role, string code, string dept) => new()
    {
        FullName = name,
        Username = user,
        PasswordHash = PasswordHasher.Hash(pwd),
        Role = role,
        EmployeeCode = code,
        Department = dept,
        Phone = "0770000000"
    };

   
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
