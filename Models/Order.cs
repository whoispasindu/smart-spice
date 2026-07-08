namespace SmartSpice.Models;

public class Order
{
    public int Id { get; set; }
    public string InvoiceNo { get; set; } = string.Empty;
    public DateTime OrderedDate { get; set; } = DateTime.Now;
    public DateTime? DispatchedDate { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public int BuyerId { get; set; }
    public Buyer? Buyer { get; set; }

    public int? HandledByEmployeeId { get; set; }
    public Employee? HandledBy { get; set; }

    public List<OrderItem> Items { get; set; } = new();

    public decimal TotalAmount => Items?.Sum(i => i.LineTotal) ?? 0;
    public double TotalWeightKg => Items?.Sum(i => i.QuantityKg) ?? 0;
}

public class OrderItem
{
    public int Id { get; set; }
    public string SpiceName { get; set; } = string.Empty;
    public double QuantityKg { get; set; }
    public decimal UnitPricePerKg { get; set; }

    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public int? SourceBatchId { get; set; }

    public decimal LineTotal => (decimal)QuantityKg * UnitPricePerKg;
}

