using OrderService.Models;

namespace OrderService.Services;

public interface ICatalogClient
{
    Task<CatalogProductDto?> GetProductAsync(int productId, CancellationToken cancellationToken = default);
}
