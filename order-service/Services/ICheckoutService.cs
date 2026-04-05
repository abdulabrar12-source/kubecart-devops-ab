using OrderService.Models;

namespace OrderService.Services;

public interface ICheckoutService
{
    Task<Order> CheckoutAsync(int userId, CancellationToken cancellationToken = default);
}
