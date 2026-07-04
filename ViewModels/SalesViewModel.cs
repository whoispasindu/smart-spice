using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using SmartSpice.Data;
using SmartSpice.Models;
using SmartSpice.Services;

namespace SmartSpice.ViewModels;

public partial class SalesViewModel : ViewModelBase, ISearchable
{
    private List<Order> _all = new();
    private string _search = string.Empty;

    public ObservableCollection<Order> Orders { get; } = new();
    public ObservableCollection<OrderItem> Lines { get; } = new();
    public ObservableCollection<Buyer> Buyers { get; } = new();
    public ObservableCollection<string> SellableProducts { get; } = new();   // raw materials + powders
    public ObservableCollection<OrderItem> NewLines { get; } = new();

    [ObservableProperty] private Order? _selected;
    [ObservableProperty] private bool _isDetail;

    [ObservableProperty] private int _totalOrders;
    [ObservableProperty] private int _inTransit;
    [ObservableProperty] private int _delivered;
    [ObservableProperty] private string _revenue = "0";

    // New-order form
    [ObservableProperty] private bool _isCreating;
    [ObservableProperty] private Buyer? _newBuyer;
    [ObservableProperty] private string _lineSpice = string.Empty;
    [ObservableProperty] private double _lineQty = 100;
    [ObservableProperty] private decimal _linePrice;

    // New-buyer form
    [ObservableProperty] private bool _isAddingBuyer;
    [ObservableProperty] private string _buyerCompany = string.Empty;
    [ObservableProperty] private string _buyerCountry = "Sri Lanka";
    [ObservableProperty] private string _buyerEmail = string.Empty;
    [ObservableProperty] private string _buyerPhone = string.Empty;
    [ObservableProperty] private bool _buyerIsExport;

    public SalesViewModel() => Title = "Dispatch Products";

    public override void Load()
    {
        using (var db = new SmartSpiceContext())
        {
            Buyers.Clear();
            foreach (var b in db.Buyers.OrderBy(b => b.CompanyName).ToList()) Buyers.Add(b);
            NewBuyer = Buyers.FirstOrDefault();
        }
        SellableProducts.Clear();
        foreach (var name in ServiceHub.Inventory.GetAll()
                     .Where(i => i.Category is InventoryCategory.RawMaterial or InventoryCategory.ProcessedPowder)
                     .Select(i => i.SpiceName).Distinct().OrderBy(n => n))
            SellableProducts.Add(name);
        LineSpice = SellableProducts.FirstOrDefault() ?? string.Empty;
        Refresh();
    }

    partial void OnLineSpiceChanged(string value)
    {
        var item = ServiceHub.Inventory.GetAll().FirstOrDefault(i => i.SpiceName == value);
        if (item != null) LinePrice = item.UnitPricePerKg;
    }

    [RelayCommand]
    private void StartCreate()
    {
        NewLines.Clear();
        IsCreating = true;
    }

    [RelayCommand] private void CancelCreate() => IsCreating = false;

    // ---------- add buyer ----------
    [RelayCommand]
    private void StartAddBuyer()
    {
        BuyerCompany = string.Empty;
        BuyerCountry = "Sri Lanka";
        BuyerEmail = string.Empty;
        BuyerPhone = string.Empty;
        BuyerIsExport = false;
        IsAddingBuyer = true;
    }

    [RelayCommand] private void CancelAddBuyer() => IsAddingBuyer = false;

    [RelayCommand]
    private void SaveBuyer()
    {
        if (string.IsNullOrWhiteSpace(BuyerCompany))
        {
            MessageBox.Show("Company name is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        int newId;
        using (var db = new SmartSpiceContext())
        {
            var buyer = new Buyer
            {
                FullName = BuyerCompany.Trim(),
                CompanyName = BuyerCompany.Trim(),
                Country = string.IsNullOrWhiteSpace(BuyerCountry) ? "Sri Lanka" : BuyerCountry.Trim(),
                IsExport = BuyerIsExport,
                Email = BuyerEmail.Trim(),
                Phone = BuyerPhone.Trim()
            };
            db.Buyers.Add(buyer);
            db.SaveChanges();
            newId = buyer.Id;
        }
        ServiceHub.Audit.Log("CREATE", "Buyer", BuyerCompany);

        // refresh the buyer list and select the new one
        using (var db = new SmartSpiceContext())
        {
            Buyers.Clear();
            foreach (var b in db.Buyers.OrderBy(b => b.CompanyName).ToList()) Buyers.Add(b);
        }
        NewBuyer = Buyers.FirstOrDefault(b => b.Id == newId);
        IsAddingBuyer = false;
    }

    [RelayCommand]
    private void AddLine()
    {
        if (string.IsNullOrWhiteSpace(LineSpice) || LineQty <= 0 || LinePrice <= 0)
        {
            MessageBox.Show("Choose a spice, a positive quantity and price.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        NewLines.Add(new OrderItem { SpiceName = LineSpice, QuantityKg = LineQty, UnitPricePerKg = LinePrice });
    }

    [RelayCommand]
    private void RemoveLine(OrderItem? line)
    {
        if (line != null) NewLines.Remove(line);
    }

    [RelayCommand]
    private void SaveOrder()
    {
        if (NewBuyer == null || NewLines.Count == 0)
        {
            MessageBox.Show("Select a buyer and add at least one line.", "Validation",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        using (var db = new SmartSpiceContext())
        {
            int n = db.Orders.Count() + 1;
            var order = new Order
            {
                InvoiceNo = $"INV-2026-{n:000}",
                BuyerId = NewBuyer.Id,
                Status = OrderStatus.Pending,
                HandledByEmployeeId = ServiceHub.Session.CurrentUser?.Id,
                Items = NewLines.Select(l => new OrderItem
                {
                    SpiceName = l.SpiceName, QuantityKg = l.QuantityKg, UnitPricePerKg = l.UnitPricePerKg
                }).ToList()
            };
            db.Orders.Add(order);
            db.SaveChanges();
            ServiceHub.Audit.Log("CREATE", "Order", $"{order.InvoiceNo} for {NewBuyer.CompanyName}");
        }
        IsCreating = false;
        Refresh();
    }

    private void Refresh()
    {
        using (var db = new SmartSpiceContext())
            _all = db.Orders.Include(o => o.Buyer).Include(o => o.Items)
                .OrderByDescending(o => o.OrderedDate).ToList();

        TotalOrders = _all.Count;
        InTransit = _all.Count(o => o.Status == OrderStatus.Dispatched);
        Delivered = _all.Count(o => o.Status == OrderStatus.Delivered);
        Revenue = $"LKR {_all.Sum(o => o.TotalAmount) / 1_000_000m:N1}M";
        ApplyFilter();
    }

    public void ApplySearch(string? text)
    {
        _search = text?.Trim() ?? string.Empty;
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        IEnumerable<Order> q = _all;
        if (_search.Length > 0)
            q = q.Where(o =>
                o.InvoiceNo.Contains(_search, StringComparison.OrdinalIgnoreCase) ||
                (o.Buyer?.CompanyName.Contains(_search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (o.Buyer?.Country.Contains(_search, StringComparison.OrdinalIgnoreCase) ?? false) ||
                o.Status.ToString().Contains(_search, StringComparison.OrdinalIgnoreCase));

        Orders.Clear();
        foreach (var o in q) Orders.Add(o);
    }

    [RelayCommand]
    private void OpenDetail(Order? order)
    {
        if (order == null) return;
        Selected = order;
        Lines.Clear();
        using var db = new SmartSpiceContext();
        foreach (var l in db.OrderItems.Where(l => l.OrderId == order.Id).ToList())
            Lines.Add(l);
        IsDetail = true;
    }

    [RelayCommand] private void CloseDetail() => IsDetail = false;

    [RelayCommand]
    private void AdvanceStatus()
    {
        if (Selected == null) return;
        if (Selected.Status is OrderStatus.Delivered or OrderStatus.Cancelled)
        {
            MessageBox.Show("This order is already closed.", "Info");
            return;
        }
        int id = Selected.Id;
        bool nowDispatched = false;
        string invoice = string.Empty;
        List<OrderItem> items = new();
        using (var db = new SmartSpiceContext())
        {
            var o = db.Orders.Include(x => x.Items).First(x => x.Id == id);
            o.Status = (OrderStatus)((int)o.Status + 1);
            if (o.Status == OrderStatus.Dispatched)
            {
                o.DispatchedDate = DateTime.Now;
                nowDispatched = true;
                invoice = o.InvoiceNo;
                items = o.Items.ToList();
            }
            db.SaveChanges();
            ServiceHub.Audit.Log("ORDER", "Order", $"{o.InvoiceNo} → {o.Status}");
        }

        // Dispatch removes the sold goods (raw materials or powders) from inventory + warehouse stock.
        if (nowDispatched)
        {
            foreach (var line in items)
            {
                double issued = ServiceHub.Inventory.IssueStockByName(
                    line.SpiceName, line.QuantityKg, $"Dispatch {invoice}");
                if (issued < line.QuantityKg - 0.01)
                    MessageBox.Show(
                        $"Only {issued:N0} kg of {line.SpiceName} was in stock; order needed {line.QuantityKg:N0} kg.",
                        "Stock shortfall", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        Refresh();
        Selected = Orders.FirstOrDefault(o => o.Id == id);
    }
}
