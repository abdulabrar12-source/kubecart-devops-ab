using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Prometheus;
using CatalogService.Config;
using CatalogService.Data;
using CatalogService.Health;
using CatalogService.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// ── Configuration (all values from environment variables) ────────────────────
var config      = AppConfig.Load();
var environment = builder.Environment.EnvironmentName;

builder.Services.AddSingleton(new DbOptions { ConnectionString = config.ConnectionString });
builder.Services.AddScoped<ICatalogRepository, CatalogRepository>();
builder.Services.AddHealthChecks()
    .AddCheck<SqlConnectionHealthCheck>("sqlserver");
builder.Services.AddHttpMetrics();
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
        var repository = scope.ServiceProvider.GetRequiredService<ICatalogRepository>();
        await repository.InitializeAsync();
        startupLogger.LogInformation("Catalog service initialized successfully.");
    }
    catch (Exception ex)
    {
        startupLogger.LogError(ex, "Failed to initialize catalog service.");
        throw;
    }
}

app.UseCors();
app.UseHttpMetrics();

// ── Routes ─────────────────────────────────────────────────────────────────────
app.MapGet("/", () => Results.Ok(new { message = "Catalog Service is running" }));

// ── Categories ─────────────────────────────────────────────────────────────────
app.MapGet("/api/catalog/categories", async (
    ICatalogRepository repo,
    CancellationToken ct) =>
{
    var categories = await repo.GetAllCategoriesAsync(ct);
    return Results.Ok(categories);
});

app.MapPost("/api/catalog/admin/categories", async (
    CreateCategoryRequest request,
    ICatalogRepository repo,
    ILoggerFactory loggerFactory,
    CancellationToken ct) =>
{
    var logger = loggerFactory.CreateLogger("CatalogEndpoints");

    if (string.IsNullOrWhiteSpace(request.Name))
        return Results.BadRequest(new { error = "Category name is required." });

    var category = await repo.CreateCategoryAsync(request.Name.Trim(), ct);
    logger.LogInformation("Created category {CategoryId}.", category.Id);
    return Results.Created($"/api/catalog/categories/{category.Id}", category);
});

// ── Products ───────────────────────────────────────────────────────────────────
app.MapGet("/api/catalog/products", async (
    int? categoryId,
    ICatalogRepository repo,
    CancellationToken ct) =>
{
    var products = await repo.GetAllProductsAsync(categoryId, ct);
    return Results.Ok(products);
});

app.MapGet("/api/catalog/products/{id:int}", async (
    int id,
    ICatalogRepository repo,
    CancellationToken ct) =>
{
    var product = await repo.GetProductByIdAsync(id, ct);
    return product is null
        ? Results.NotFound(new { error = $"Product {id} not found." })
        : Results.Ok(product);
});

app.MapPost("/api/catalog/admin/products", async (
    CreateProductRequest request,
    ICatalogRepository repo,
    ILoggerFactory loggerFactory,
    CancellationToken ct) =>
{
    var logger = loggerFactory.CreateLogger("CatalogEndpoints");

    if (string.IsNullOrWhiteSpace(request.Name))
        return Results.BadRequest(new { error = "Product name is required." });
    if (request.Price <= 0)
        return Results.BadRequest(new { error = "Price must be greater than zero." });
    if (request.CategoryId <= 0)
        return Results.BadRequest(new { error = "CategoryId is required." });

    var product = await repo.CreateProductAsync(request, ct);
    logger.LogInformation("Created product {ProductId}.", product.Id);
    return Results.Created($"/api/catalog/products/{product.Id}", product);
});

app.MapPut("/api/catalog/admin/products/{id:int}", async (
    int id,
    UpdateProductRequest request,
    ICatalogRepository repo,
    ILoggerFactory loggerFactory,
    CancellationToken ct) =>
{
    var logger = loggerFactory.CreateLogger("CatalogEndpoints");

    var product = await repo.UpdateProductAsync(id, request, ct);
    if (product is null)
    {
        logger.LogWarning("Update failed: product {ProductId} not found.", id);
        return Results.NotFound(new { error = $"Product {id} not found." });
    }
    return Results.Ok(product);
});

app.MapDelete("/api/catalog/admin/products/{id:int}", async (
    int id,
    ICatalogRepository repo,
    ILoggerFactory loggerFactory,
    CancellationToken ct) =>
{
    var logger = loggerFactory.CreateLogger("CatalogEndpoints");

    var deleted = await repo.DeleteProductAsync(id, ct);
    if (!deleted)
    {
        logger.LogWarning("Delete failed: product {ProductId} not found.", id);
        return Results.NotFound(new { error = $"Product {id} not found." });
    }
    logger.LogInformation("Deleted product {ProductId}.", id);
    return Results.NoContent();
});

// ── Admin: product image ──────────────────────────────────────────────────────
app.MapPost("/api/catalog/admin/products/{id:int}/images", async (
    int id,
    UpdateProductImageRequest request,
    ICatalogRepository repo,
    ILoggerFactory loggerFactory,
    CancellationToken ct) =>
{
    var logger = loggerFactory.CreateLogger("CatalogEndpoints");

    if (string.IsNullOrWhiteSpace(request.ImageUrl))
        return Results.BadRequest(new { error = "ImageUrl is required." });

    var product = await repo.UpdateProductAsync(id, new UpdateProductRequest { ImageUrl = request.ImageUrl }, ct);
    if (product is null)
    {
        logger.LogWarning("Image update failed: product {ProductId} not found.", id);
        return Results.NotFound(new { error = $"Product {id} not found." });
    }
    logger.LogInformation("Updated image for product {ProductId}.", id);
    return Results.Ok(product);
});

app.MapMetrics("/metrics");
app.MapHealthChecks("/health", new HealthCheckOptions());

app.Run();
