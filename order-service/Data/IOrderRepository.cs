using OrderService.Models;

namespace OrderService.Data;

public interface IOrderRepository
{
    Task InitializeAsync(CancellationToken cancellationToken = default);

    // ── Orders ────────────────────────────────────────────────────────────────
    Task<Order> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default);
    Task<Order?> GetOrderByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderSummary>> GetOrdersByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<Order?> UpdateOrderStatusAsync(int id, string status, CancellationToken cancellationToken = default);

    // ── Cart ──────────────────────────────────────────────────────────────────
    Task<IReadOnlyList<CartItemDb>> GetCartItemsAsync(int userId, CancellationToken cancellationToken = default);
    Task<CartItemDb?> GetCartItemAsync(int cartItemId, CancellationToken cancellationToken = default);
    Task<CartItemDb> AddToCartAsync(AddToCartRequest request, CatalogProductDto product, CancellationToken cancellationToken = default);
    Task<CartItemDb?> UpdateCartItemAsync(int cartItemId, int quantity, CancellationToken cancellationToken = default);
    Task<bool> DeleteCartItemAsync(int cartItemId, CancellationToken cancellationToken = default);
    /// <summary>Update a cart row identified by (userId, productId) — used by PUT /api/orders/cart/items/{productId}.</summary>
    Task<CartItemDb?> UpdateCartItemByProductAsync(int userId, int productId, int quantity, CancellationToken cancellationToken = default);
    /// <summary>Delete a cart row identified by (userId, productId) — used by DELETE /api/orders/cart/items/{productId}.</summary>
    Task<bool> DeleteCartItemByProductAsync(int userId, int productId, CancellationToken cancellationToken = default);
    Task ClearCartAsync(int userId, CancellationToken cancellationToken = default);
}
