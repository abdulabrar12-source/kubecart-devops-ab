namespace OrderService.Models;

/// <summary>
/// Request body for POST /api/orders/checkout
/// </summary>
public sealed class CheckoutRequest
{
    public int UserId { get; set; }
}
