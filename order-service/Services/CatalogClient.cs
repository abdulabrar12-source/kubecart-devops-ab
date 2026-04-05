using System.Net;
using System.Net.Http.Json;
using OrderService.Models;

namespace OrderService.Services;

public sealed class CatalogClient : ICatalogClient
{
    private readonly HttpClient _http;
    private readonly ILogger<CatalogClient> _logger;

    public CatalogClient(HttpClient http, ILogger<CatalogClient> logger)
    {
        _http   = http;
        _logger = logger;
    }

    public async Task<CatalogProductDto?> GetProductAsync(int productId, CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await _http.GetAsync(
                $"/api/catalog/products/{productId}", cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex,
                "Network error reaching catalog-service for product {ProductId}.", productId);
            throw new CatalogServiceException(
                $"Catalog service is unreachable (product {productId}).", ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "Catalog-service timed out for product {ProductId}.", productId);
            throw new CatalogServiceException(
                $"Catalog service timed out (product {productId}).", ex);
        }

        // 404 means the product genuinely does not exist — return null so callers
        // can surface a meaningful 404, not a 503.
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        // Any other non-2xx (5xx, 429, etc.) is a transient upstream failure.
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Catalog-service returned {StatusCode} for product {ProductId}.",
                (int)response.StatusCode, productId);
            throw new CatalogServiceException(
                $"Catalog service returned HTTP {(int)response.StatusCode} for product {productId}.");
        }

        return await response.Content
            .ReadFromJsonAsync<CatalogProductDto>(cancellationToken: cancellationToken);
    }
}
