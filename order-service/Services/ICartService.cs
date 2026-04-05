using OrderService.Models;

namespace OrderService.Services;

public interface ICartService
{
    Task<IReadOnlyList<CartItemDb>> GetCartAsync(int userId, CancellationToken cancellationToken = default);
    Task<CartItemDb> AddToCartAsync(AddToCartRequest request, CancellationToken cancellationToken = default);
    // Original cartItemId-based methods (kept for internal use)
    Task<CartItemDb?> UpdateCartItemAsync(int cartItemId, int quantity, CancellationToken cancellationToken = default);
    Task<bool> RemoveCartItemAsync(int cartItemId, CancellationToken cancellationToken = default);
    // Product-keyed methods used by PUT /api/orders/cart/items/{productId} and DELETE /api/orders/cart/items/{productId}
    Task<CartItemDb?> UpdateCartItemByProductAsync(int userId, int productId, int quantity, CancellationToken cancellationToken = default);
    Task<bool> RemoveCartItemByProductAsync(int userId, int productId, CancellationToken cancellationToken = default);
    Task ClearCartAsync(int userId, CancellationToken cancellationToken = default);
}
