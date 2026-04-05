using CatalogService.Models;

namespace CatalogService.Data;

public interface ICatalogRepository
{
    Task InitializeAsync(CancellationToken cancellationToken = default);

    // Categories
    Task<IReadOnlyList<Category>> GetAllCategoriesAsync(CancellationToken cancellationToken = default);
    Task<Category> CreateCategoryAsync(string name, CancellationToken cancellationToken = default);

    // Products
    Task<IReadOnlyList<Product>> GetAllProductsAsync(int? categoryId = null, CancellationToken cancellationToken = default);
    Task<Product?> GetProductByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Product> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<Product?> UpdateProductAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteProductAsync(int id, CancellationToken cancellationToken = default);
}
