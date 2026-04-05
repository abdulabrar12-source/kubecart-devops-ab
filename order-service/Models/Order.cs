namespace OrderService.Models;

public sealed class Order
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}
