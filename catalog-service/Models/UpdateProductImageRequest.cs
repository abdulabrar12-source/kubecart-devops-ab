namespace CatalogService.Models;

/// <summary>
/// Request body for POST /api/catalog/admin/products/{id}/images
/// Updates only the ImageUrl field of an existing product.
/// </summary>
public sealed class UpdateProductImageRequest
{
    public string ImageUrl { get; set; } = string.Empty;
}
