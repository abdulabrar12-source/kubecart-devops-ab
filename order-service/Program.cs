using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Prometheus;
using OrderService.Config;
using OrderService.Data;
using OrderService.Health;
using OrderService.Models;
using OrderService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// ── Configuration (all values from environment variables) ────────────────────
var config      = AppConfig.Load();
var environment = builder.Environment.EnvironmentName;

builder.Services.AddSingleton(new DbOptions { ConnectionString = config.ConnectionString });
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

// ── Catalog HTTP client (typed) ──────────────────────────────────────────────
builder.Services.AddHttpClient<ICatalogClient, CatalogClient>(client =>
{
    client.BaseAddress = new Uri(config.CatalogServiceUrl);
    // Keep well under the default 100 s — a catalog call is on the hot path
    // for AddToCart and Checkout. CatalogClient converts TaskCanceledException
    // (timeout) to CatalogServiceException so callers get a 503, not a 500.
    client.Timeout = TimeSpan.FromSeconds(5);
});
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<ICheckoutService, CheckoutService>();
builder.Services.AddHealthChecks()
    .AddCheck<SqlConnectionHealthCheck>("sqlserver");
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

// ── DB Initialisation ──────────────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var startupLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    try
    {
        startupLogger.LogInformation("Environment: {Environment}", environment);
        startupLogger.LogInformation("Connecting to DB: {DbName}", config.DbName);
        var repository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
        await repository.InitializeAsync();
        startupLogger.LogInformation("Order service initialized successfully.");
    }
    catch (Exception ex)
    {
        startupLogger.LogError(ex, "Failed to initialize order service.");
        throw;
    }
}

app.UseCors();
app.UseHttpMetrics();

// ── Routes ─────────────────────────────────────────────────────────────────────
app.MapGet("/", () => Results.Ok(new { message = "Order Service is running" }));

app.MapGet("/api/orders/{id:int}", async (
    int id,
    IOrderRepository repo,
    CancellationToken ct) =>
{
    var order = await repo.GetOrderByIdAsync(id, ct);
    return order is null
        ? Results.NotFound(new { error = $"Order {id} not found." })
        : Results.Ok(order);
});

app.MapGet("/api/orders", async (
    int userId,
    IOrderRepository repo,
    CancellationToken ct) =>
{
    if (userId <= 0)
        return Results.BadRequest(new { error = "userId query parameter is required." });
    var orders = await repo.GetOrdersByUserIdAsync(userId, ct);
    return Results.Ok(orders);
});

app.MapPut("/api/orders/admin/{orderId:int}/status", async (
    int orderId,
    UpdateOrderStatusRequest request,
    IOrderRepository repo,
    ILoggerFactory loggerFactory,
    CancellationToken ct) =>
{
    var logger = loggerFactory.CreateLogger("OrderEndpoints");

    if (string.IsNullOrWhiteSpace(request.Status))
        return Results.BadRequest(new { error = "Status is required." });

    var validStatuses = new[] { "Pending", "Confirmed", "Shipped", "Cancelled" };
    if (!validStatuses.Contains(request.Status))
        return Results.BadRequest(new { error = $"Status must be one of: {string.Join(", ", validStatuses)}." });

    var order = await repo.UpdateOrderStatusAsync(orderId, request.Status, ct);
    if (order is null)
    {
        logger.LogWarning("Update failed: order {OrderId} not found.", orderId);
        return Results.NotFound(new { error = $"Order {orderId} not found." });
    }
    logger.LogInformation("Order {OrderId} status updated to {Status}.", orderId, request.Status);
    return Results.Ok(order);
});

app.MapMetrics("/metrics");
app.MapHealthChecks("/health", new HealthCheckOptions());

// ── Cart routes ───────────────────────────────────────────────────────────────

app.MapGet("/api/orders/cart", async (
    int userId,
    ICartService cart,
    CancellationToken ct) =>
{
    if (userId <= 0)
        return Results.BadRequest(new { error = "userId query parameter is required." });
    var items = await cart.GetCartAsync(userId, ct);
    return Results.Ok(items);
});

app.MapPost("/api/orders/cart/items", async (
    AddToCartRequest request,
    ICartService cart,
    ILoggerFactory loggerFactory,
    CancellationToken ct) =>
{
    var logger = loggerFactory.CreateLogger("CartEndpoints");

    if (request.UserId <= 0)
        return Results.BadRequest(new { error = "UserId is required." });
    if (request.ProductId <= 0)
        return Results.BadRequest(new { error = "ProductId is required." });
    if (request.Quantity <= 0)
        return Results.BadRequest(new { error = "Quantity must be greater than 0." });

    try
    {
        var item = await cart.AddToCartAsync(request, ct);
        return Results.Created($"/api/orders/cart?userId={request.UserId}", item);
    }
    catch (KeyNotFoundException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        logger.LogWarning(ex, "Stock check failed for product {ProductId}.", request.ProductId);
        return Results.Conflict(new { error = ex.Message });
    }
    catch (CatalogServiceException ex)
    {
        logger.LogError(ex, "Catalog service unavailable while adding product {ProductId} to cart.", request.ProductId);
        return Results.Problem(
            title: "Catalog service unavailable.",
            detail: ex.Message,
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
});

app.MapPut("/api/orders/cart/items/{productId:int}", async (
    int productId,
    int userId,
    UpdateCartItemRequest request,
    ICartService cart,
    CancellationToken ct) =>
{
    if (userId <= 0)
        return Results.BadRequest(new { error = "userId query parameter is required." });
    if (request.Quantity <= 0)
        return Results.BadRequest(new { error = "Quantity must be greater than 0." });

    var updated = await cart.UpdateCartItemByProductAsync(userId, productId, request.Quantity, ct);
    return updated is null
        ? Results.NotFound(new { error = $"Product {productId} not found in cart for user {userId}." })
        : Results.Ok(updated);
});

app.MapDelete("/api/orders/cart/items/{productId:int}", async (
    int productId,
    int userId,
    ICartService cart,
    CancellationToken ct) =>
{
    if (userId <= 0)
        return Results.BadRequest(new { error = "userId query parameter is required." });

    var deleted = await cart.RemoveCartItemByProductAsync(userId, productId, ct);
    return deleted
        ? Results.NoContent()
        : Results.NotFound(new { error = $"Product {productId} not found in cart for user {userId}." });
});

// ── Checkout route ──────────────────────────────────────────────────────────

app.MapPost("/api/orders/checkout", async (
    CheckoutRequest request,
    ICheckoutService checkout,
    ILoggerFactory loggerFactory,
    CancellationToken ct) =>
{
    var logger = loggerFactory.CreateLogger("CheckoutEndpoints");

    if (request.UserId <= 0)
        return Results.BadRequest(new { error = "UserId is required." });

    try
    {
        var order = await checkout.CheckoutAsync(request.UserId, ct);
        logger.LogInformation("User {UserId} checked out, order {OrderId} created.", request.UserId, order.Id);
        return Results.Created($"/api/orders/{order.Id}", order);
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.Run();
