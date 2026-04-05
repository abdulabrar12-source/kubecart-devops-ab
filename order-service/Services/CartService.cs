using OrderService.Data;
using OrderService.Models;

namespace OrderService.Services;

public sealed class CartService : ICartService
{
    private readonly IOrderRepository _repo;
    private readonly ICatalogClient _catalogClient;
    private readonly ILogger<CartService> _logger;

    public CartService(IOrderRepository repo, ICatalogClient catalogClient, ILogger<CartService> logger)
    {
        _repo          = repo;
        _catalogClient = catalogClient;
        _logger        = logger;
    }

    public Task<IReadOnlyList<CartItemDb>> GetCartAsync(int userId, CancellationToken cancellationToken = default)
        => _repo.GetCartItemsAsync(userId, cancellationToken);

    public async Task<CartItemDb> AddToCartAsync(AddToCartRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0.");

        var product = await _catalogClient.GetProductAsync(request.ProductId, cancellationToken)
            ?? throw new KeyNotFoundException($"Product {request.ProductId} not found in catalog.");

        if (product.StockQuantity < request.Quantity)
            throw new InvalidOperationException(
                $"Insufficient stock for '{product.Name}'. Available: {product.StockQuantity}.");

        var item = await _repo.AddToCartAsync(request, product, cancellationToken);
        _logger.LogInformation(
            "User {UserId} added {Qty}x '{Product}' to cart (CartItem {CartItemId}).",
            request.UserId, request.Quantity, product.Name, item.Id);
        return item;
    }

    public async Task<CartItemDb?> UpdateCartItemAsync(int cartItemId, int quantity, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0.");

        return await _repo.UpdateCartItemAsync(cartItemId, quantity, cancellationToken);
    }

    public Task<bool> RemoveCartItemAsync(int cartItemId, CancellationToken cancellationToken = default)
        => _repo.DeleteCartItemAsync(cartItemId, cancellationToken);

    public async Task<CartItemDb?> UpdateCartItemByProductAsync(
        int userId, int productId, int quantity, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0.");

        return await _repo.UpdateCartItemByProductAsync(userId, productId, quantity, cancellationToken);
    }

    public Task<bool> RemoveCartItemByProductAsync(int userId, int productId, CancellationToken cancellationToken = default)
        => _repo.DeleteCartItemByProductAsync(userId, productId, cancellationToken);

    public Task ClearCartAsync(int userId, CancellationToken cancellationToken = default)
        => _repo.ClearCartAsync(userId, cancellationToken);
}
