namespace OrderService.Models;

/// <summary>
/// DTO for deserialising a product response from catalog-service.
/// </summary>
public sealed class CatalogProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public int CategoryId { get; set; }
}
