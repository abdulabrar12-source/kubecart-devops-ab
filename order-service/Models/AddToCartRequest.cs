namespace OrderService.Models;

public sealed class AddToCartRequest
{
    public int UserId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}
