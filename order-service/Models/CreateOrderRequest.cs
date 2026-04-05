namespace OrderService.Models;

public sealed class CreateOrderRequest
{
    public int UserId { get; set; }
    public List<CartItem> Items { get; set; } = new();
}
