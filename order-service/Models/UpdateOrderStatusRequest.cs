namespace OrderService.Models;

public sealed class UpdateOrderStatusRequest
{
    /// <summary>Allowed values: Pending, Confirmed, Shipped, Cancelled</summary>
    public string Status { get; set; } = string.Empty;
}
