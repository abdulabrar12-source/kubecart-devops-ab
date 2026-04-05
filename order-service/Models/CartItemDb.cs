namespace OrderService.Models;

/// <summary>
/// A cart row persisted in the CartItems table.
/// </summary>
public sealed class CartItemDb
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public DateTime AddedAtUtc { get; set; }
}
