using OrderService.Data;
using OrderService.Models;

namespace OrderService.Services;

public sealed class CheckoutService : ICheckoutService
{
    private readonly IOrderRepository _repo;
    private readonly ICatalogClient _catalogClient;
    private readonly ILogger<CheckoutService> _logger;

    public CheckoutService(IOrderRepository repo, ICatalogClient catalogClient, ILogger<CheckoutService> logger)
    {
        _repo          = repo;
        _catalogClient = catalogClient;
        _logger        = logger;
    }

    public async Task<Order> CheckoutAsync(int userId, CancellationToken cancellationToken = default)
    {
        var cartItems = await _repo.GetCartItemsAsync(userId, cancellationToken);
        if (cartItems.Count == 0)
            throw new InvalidOperationException("Cart is empty. Add items before checking out.");

        var orderItems = new List<CartItem>();
        foreach (var cartItem in cartItems)
        {
            // Attempt to fetch the live product from catalog.
            // CatalogServiceException (network/timeout/5xx) is caught here so a transient
            // catalog outage does not prevent checkout — we fall back to the cart-stored price.
            // A genuine 404 returns null without throwing, which should not happen if the
            // catalog was healthy when the item was added to the cart.
            CatalogProductDto? product = null;
            try
            {
                product = await _catalogClient.GetProductAsync(cartItem.ProductId, cancellationToken);
            }
            catch (CatalogServiceException ex)
            {
                _logger.LogWarning(ex,
                    "Catalog unreachable for product {ProductId} during checkout; " +
                    "proceeding with cart-stored price {Price:C}.",
                    cartItem.ProductId, cartItem.UnitPrice);
            }

            if (product is not null)
            {
                // Validate that stock is still sufficient at checkout time.
                // This catches the window between AddToCart and Checkout where stock may have fallen.
                if (product.StockQuantity < cartItem.Quantity)
                    throw new InvalidOperationException(
                        $"Insufficient stock for '{product.Name}'. " +
                        $"Available: {product.StockQuantity}, requested: {cartItem.Quantity}.");
            }

            orderItems.Add(new CartItem
            {
                ProductId   = cartItem.ProductId,
                ProductName = cartItem.ProductName,
                // Use the authoritative catalog price when available.
                // Fall back to the snapshot stored at AddToCart time when catalog is unreachable.
                UnitPrice   = product?.Price ?? cartItem.UnitPrice,
                Quantity    = cartItem.Quantity
            });
        }

        var createRequest = new CreateOrderRequest
        {
            UserId = userId,
            Items  = orderItems
        };

        var order = await _repo.CreateOrderAsync(createRequest, cancellationToken);
        await _repo.ClearCartAsync(userId, cancellationToken);

        _logger.LogInformation(
            "Checkout complete for user {UserId}: order {OrderId} created with {ItemCount} item(s).",
            userId, order.Id, orderItems.Count);

        return order;
    }
}
