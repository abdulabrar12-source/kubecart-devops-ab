namespace OrderService.Models;

/// <summary>
/// Lightweight order projection used in list responses (no items collection).
/// </summary>
public sealed class OrderSummary
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int ItemCount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
