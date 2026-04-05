namespace OrderService.Services;

/// <summary>
/// Thrown by <see cref="CatalogClient"/> when catalog-service is unreachable,
/// times out, or returns an unexpected non-404 HTTP error.
///
/// Callers must distinguish this from <see cref="KeyNotFoundException"/>
/// (product genuinely does not exist — catalog returned 404) so they can
/// surface the correct HTTP status to the frontend:
///   - CatalogServiceException  →  503 Service Unavailable  (upstream failure)
///   - KeyNotFoundException     →  404 Not Found            (product missing)
///   - InvalidOperationException →  409 Conflict            (stock exhausted)
/// </summary>
public sealed class CatalogServiceException : Exception
{
    public CatalogServiceException(string message)
        : base(message) { }

    public CatalogServiceException(string message, Exception inner)
        : base(message, inner) { }
}
