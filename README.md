# Smart Spice 🌶️

A desktop management system for a spice processing and export business, built with WPF on .NET 10. It covers the full workflow from farmer supply intake through processing, quality control, warehousing, and sales — with forecasting and reporting on top.

## Features

- **Dashboard** — at-a-glance overview of inventory, batches, sales, and alerts
- **Inventory Management** — track spice stock levels across warehouses
- **Batch Tracking** — manage spice batches from intake through processing
- **Quality Control** — record and review quality inspections per batch
- **Sales & Orders** — manage buyers, orders, and sales records
- **Sales Forecasting** — projected sales trends based on historical monthly data
- **Warehouses** — manage storage locations and capacity (raw, processed, export holding)
- **Farmers & Suppliers** — maintain farmer/supplier records
- **Employees & Roles** — role-based accounts (Administrator, Manager, Warehouse Staff, Quality Inspector, Sales Officer)
- **Reports** — generate and export reports (CSV export supported)
- **Notifications** — in-app alerts for important events
- **Audit Logging** — actions are recorded for accountability

## Tech Stack

| Layer | Technology |
|---|---|
| UI | WPF (.NET 10, Windows) |
| Architecture | MVVM via [CommunityToolkit.Mvvm](https://github.com/CommunityToolkit/dotnet) |
| Database | SQLite via Entity Framework Core 10 |
| Language | C# with nullable reference types enabled |

## Getting Started

### Prerequisites

- Windows 10/11
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Visual Studio 2026 (or run from the command line)

### Run

```bash
git clone https://github.com/whoispasindu/smart-spice.git
cd smart-spice/smart-spice
dotnet run
```

Or open `smart-spice.slnx` in Visual Studio and press **F5**.

On first launch the app creates and seeds a SQLite database automatically at
`%LocalAppData%\SmartSpice\smartspice.db` — no setup needed.

### Default Login Accounts

| Username | Password | Role |
|---|---|---|
| `admin` | `admin123` | Administrator |
| `quality` | `quality123` | Quality Inspector |
| `sales` | `sales123` | Sales Officer |

> ⚠️ These are seeded demo credentials — change them before any real-world use.

## Project Structure

```
smart-spice/
├── Assets/        # Images, icons, and sample data (sales_monthly.csv)
├── Data/          # EF Core DbContext and database seeder
├── Helpers/       # Converters, CSV export, password hashing, chart geometry
├── Models/        # Entity classes (Batch, Farmer, Order, Inventory, ...)
├── Services/      # Auth, inventory, notifications, forecasting, auditing
├── Themes/        # Shared XAML styles and resources
├── ViewModels/    # MVVM view models
└── Views/         # XAML views (Dashboard, Inventory, Sales, Reports, ...)
```

## Resetting the Database

Delete `%LocalAppData%\SmartSpice\smartspice.db` and restart the app — it will recreate and reseed automatically.
```