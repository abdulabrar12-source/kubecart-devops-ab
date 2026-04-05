using Dapper;
using Microsoft.Data.SqlClient;
using CatalogService.Models;

namespace CatalogService.Data;

public sealed class CatalogRepository : ICatalogRepository
{
    private readonly DbOptions _dbOptions;
    private readonly ILogger<CatalogRepository> _logger;

    public CatalogRepository(DbOptions dbOptions, ILogger<CatalogRepository> logger)
    {
        _dbOptions = dbOptions;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Categories')
            BEGIN
                CREATE TABLE Categories (
                    Id   INT IDENTITY(1,1) PRIMARY KEY,
                    Name NVARCHAR(100) NOT NULL,
                    Slug NVARCHAR(100) NOT NULL UNIQUE
                );
            END

            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Products')
            BEGIN
                CREATE TABLE Products (
                    Id            INT IDENTITY(1,1) PRIMARY KEY,
                    CategoryId    INT            NOT NULL REFERENCES Categories(Id),
                    Name          NVARCHAR(200)  NOT NULL,
                    Description   NVARCHAR(2000) NOT NULL CONSTRAINT DF_Products_Description DEFAULT(''),
                    Price         DECIMAL(18,2)  NOT NULL,
                    StockQuantity INT            NOT NULL CONSTRAINT DF_Products_Stock DEFAULT(0),
                    ImageUrl      NVARCHAR(500)  NOT NULL CONSTRAINT DF_Products_ImageUrl DEFAULT(''),
                    CreatedAtUtc  DATETIME2      NOT NULL CONSTRAINT DF_Products_CreatedAtUtc DEFAULT(SYSUTCDATETIME())
                );
            END
            """;

        await using var connection = new SqlConnection(_dbOptions.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: cancellationToken));
        _logger.LogInformation("Ensured Categories and Products tables exist.");
    }

    public async Task<IReadOnlyList<Category>> GetAllCategoriesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT Id, Name, Slug FROM Categories ORDER BY Name;";

        await using var connection = new SqlConnection(_dbOptions.ConnectionString);
        var rows = await connection.QueryAsync<Category>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));
        return rows.ToList();
    }

    public async Task<Category> CreateCategoryAsync(string name, CancellationToken cancellationToken = default)
    {
        var slug = name.ToLower().Trim().Replace(" ", "-");
        const string sql = """
            INSERT INTO Categories (Name, Slug)
            OUTPUT INSERTED.Id, INSERTED.Name, INSERTED.Slug
            VALUES (@Name, @Slug);
            """;

        await using var connection = new SqlConnection(_dbOptions.ConnectionString);
        var category = await connection.QuerySingleAsync<Category>(
            new CommandDefinition(sql, new { Name = name, Slug = slug }, cancellationToken: cancellationToken));
        _logger.LogInformation("Created category {CategoryId}: {Name}.", category.Id, category.Name);
        return category;
    }

    public async Task<IReadOnlyList<Product>> GetAllProductsAsync(int? categoryId = null, CancellationToken cancellationToken = default)
    {
        var sql = categoryId.HasValue
            ? """
              SELECT Id, CategoryId, Name, Description, Price, StockQuantity, ImageUrl, CreatedAtUtc
              FROM Products WHERE CategoryId = @CategoryId ORDER BY Id DESC;
              """
            : """
              SELECT Id, CategoryId, Name, Description, Price, StockQuantity, ImageUrl, CreatedAtUtc
              FROM Products ORDER BY Id DESC;
              """;

        await using var connection = new SqlConnection(_dbOptions.ConnectionString);
        var rows = await connection.QueryAsync<Product>(
            new CommandDefinition(sql,
                categoryId.HasValue ? new { CategoryId = categoryId.Value } : null,
                cancellationToken: cancellationToken));
        return rows.ToList();
    }

    public async Task<Product?> GetProductByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, CategoryId, Name, Description, Price, StockQuantity, ImageUrl, CreatedAtUtc
            FROM Products WHERE Id = @Id;
            """;

        await using var connection = new SqlConnection(_dbOptions.ConnectionString);
        return await connection.QuerySingleOrDefaultAsync<Product>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task<Product> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO Products (CategoryId, Name, Description, Price, StockQuantity, ImageUrl)
            OUTPUT INSERTED.Id, INSERTED.CategoryId, INSERTED.Name, INSERTED.Description,
                   INSERTED.Price, INSERTED.StockQuantity, INSERTED.ImageUrl, INSERTED.CreatedAtUtc
            VALUES (@CategoryId, @Name, @Description, @Price, @StockQuantity, @ImageUrl);
            """;

        await using var connection = new SqlConnection(_dbOptions.ConnectionString);
        var product = await connection.QuerySingleAsync<Product>(
            new CommandDefinition(sql, request, cancellationToken: cancellationToken));
        _logger.LogInformation("Created product {ProductId}: {Name}.", product.Id, product.Name);
        return product;
    }

    public async Task<Product?> UpdateProductAsync(int id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE Products
            SET Name          = COALESCE(@Name,          Name),
                Description   = COALESCE(@Description,   Description),
                Price         = COALESCE(@Price,         Price),
                StockQuantity = COALESCE(@StockQuantity, StockQuantity),
                ImageUrl      = COALESCE(@ImageUrl,      ImageUrl)
            OUTPUT INSERTED.Id, INSERTED.CategoryId, INSERTED.Name, INSERTED.Description,
                   INSERTED.Price, INSERTED.StockQuantity, INSERTED.ImageUrl, INSERTED.CreatedAtUtc
            WHERE Id = @Id;
            """;

        await using var connection = new SqlConnection(_dbOptions.ConnectionString);
        var product = await connection.QuerySingleOrDefaultAsync<Product>(
            new CommandDefinition(sql,
                new { Id = id, request.Name, request.Description, request.Price, request.StockQuantity, request.ImageUrl },
                cancellationToken: cancellationToken));

        if (product is not null)
            _logger.LogInformation("Updated product {ProductId}.", id);
        return product;
    }

    public async Task<bool> DeleteProductAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM Products WHERE Id = @Id;";

        await using var connection = new SqlConnection(_dbOptions.ConnectionString);
        var rows = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
        if (rows > 0)
            _logger.LogInformation("Deleted product {ProductId}.", id);
        return rows > 0;
    }
}
