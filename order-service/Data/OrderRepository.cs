using Dapper;
using Microsoft.Data.SqlClient;
using OrderService.Models;

namespace OrderService.Data;

public sealed class OrderRepository : IOrderRepository
{
    private readonly DbOptions _dbOptions;
    private readonly ILogger<OrderRepository> _logger;

    public OrderRepository(DbOptions dbOptions, ILogger<OrderRepository> logger)
    {
        _dbOptions = dbOptions;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Orders')
            BEGIN
                CREATE TABLE Orders (
                    Id           INT IDENTITY(1,1) PRIMARY KEY,
                    UserId       INT           NOT NULL,
                    Status       NVARCHAR(50)  NOT NULL CONSTRAINT DF_Orders_Status DEFAULT('Pending'),
                    TotalAmount  DECIMAL(18,2) NOT NULL,
                    CreatedAtUtc DATETIME2     NOT NULL CONSTRAINT DF_Orders_CreatedAtUtc DEFAULT(SYSUTCDATETIME())
                );
            END

            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OrderItems')
            BEGIN
                CREATE TABLE OrderItems (
                    Id          INT IDENTITY(1,1) PRIMARY KEY,
                    OrderId     INT           NOT NULL REFERENCES Orders(Id),
                    ProductId   INT           NOT NULL,
                    ProductName NVARCHAR(200) NOT NULL,
                    UnitPrice   DECIMAL(18,2) NOT NULL,
                    Quantity    INT           NOT NULL
                );
            END

            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CartItems')
            BEGIN
                CREATE TABLE CartItems (
                    Id          INT IDENTITY(1,1) PRIMARY KEY,
                    UserId      INT           NOT NULL,
                    ProductId   INT           NOT NULL,
                    ProductName NVARCHAR(200) NOT NULL,
                    ImageUrl    NVARCHAR(500) NOT NULL CONSTRAINT DF_CartItems_ImageUrl DEFAULT(''),
                    UnitPrice   DECIMAL(18,2) NOT NULL,
                    Quantity    INT           NOT NULL,
                    AddedAtUtc  DATETIME2     NOT NULL CONSTRAINT DF_CartItems_AddedAtUtc DEFAULT(SYSUTCDATETIME()),
                    CONSTRAINT UQ_CartItems_UserProduct UNIQUE (UserId, ProductId)
                );
            END
            """;

        await using var connection = new SqlConnection(_dbOptions.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: cancellationToken));
        _logger.LogInformation("Ensured Orders, OrderItems, and CartItems tables exist.");
    }

    public async Task<Order> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        var total = request.Items.Sum(i => i.UnitPrice * i.Quantity);

        const string insertOrder = """
            INSERT INTO Orders (UserId, TotalAmount)
            OUTPUT INSERTED.Id, INSERTED.UserId, INSERTED.Status, INSERTED.TotalAmount, INSERTED.CreatedAtUtc
            VALUES (@UserId, @TotalAmount);
            """;

        const string insertItem = """
            INSERT INTO OrderItems (OrderId, ProductId, ProductName, UnitPrice, Quantity)
            OUTPUT INSERTED.Id, INSERTED.OrderId, INSERTED.ProductId,
                   INSERTED.ProductName, INSERTED.UnitPrice, INSERTED.Quantity
            VALUES (@OrderId, @ProductId, @ProductName, @UnitPrice, @Quantity);
            """;

        await using var connection = new SqlConnection(_dbOptions.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        var order = await connection.QuerySingleAsync<Order>(
            new CommandDefinition(insertOrder,
                new { request.UserId, TotalAmount = total },
                cancellationToken: cancellationToken));

        var items = new List<OrderItem>();
        foreach (var item in request.Items)
        {
            var inserted = await connection.QuerySingleAsync<OrderItem>(
                new CommandDefinition(insertItem,
                    new { OrderId = order.Id, item.ProductId, item.ProductName, item.UnitPrice, item.Quantity },
                    cancellationToken: cancellationToken));
            items.Add(inserted);
        }

        order.Items = items;
        _logger.LogInformation("Created order {OrderId} with {ItemCount} items for user {UserId}.",
            order.Id, items.Count, order.UserId);
        return order;
    }

    public async Task<Order?> GetOrderByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT o.Id, o.UserId, o.Status, o.TotalAmount, o.CreatedAtUtc,
                   i.Id, i.OrderId, i.ProductId, i.ProductName, i.UnitPrice, i.Quantity
            FROM Orders o
            LEFT JOIN OrderItems i ON i.OrderId = o.Id
            WHERE o.Id = @Id;
            """;

        await using var connection = new SqlConnection(_dbOptions.ConnectionString);
        Order? order = null;

        await connection.QueryAsync<Order, OrderItem, Order>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken),
            (o, item) =>
            {
                order ??= o;
                if (item is not null && item.Id > 0) order.Items.Add(item);
                return order;
            },
            splitOn: "Id");

        return order;
    }

    public async Task<IReadOnlyList<OrderSummary>> GetOrdersByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT o.Id,
                   o.UserId,
                   o.Status,
                   o.TotalAmount,
                   o.CreatedAtUtc,
                   COUNT(i.Id) AS ItemCount
            FROM Orders o
            LEFT JOIN OrderItems i ON i.OrderId = o.Id
            WHERE o.UserId = @UserId
            GROUP BY o.Id, o.UserId, o.Status, o.TotalAmount, o.CreatedAtUtc
            ORDER BY o.Id DESC;
            """;

        await using var connection = new SqlConnection(_dbOptions.ConnectionString);
        var results = await connection.QueryAsync<OrderSummary>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken));
        return results.ToList();
    }

    public async Task<Order?> UpdateOrderStatusAsync(int id, string status, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE Orders SET Status = @Status
            OUTPUT INSERTED.Id, INSERTED.UserId, INSERTED.Status, INSERTED.TotalAmount, INSERTED.CreatedAtUtc
            WHERE Id = @Id;
            """;

        await using var connection = new SqlConnection(_dbOptions.ConnectionString);
        var order = await connection.QuerySingleOrDefaultAsync<Order>(
            new CommandDefinition(sql, new { Id = id, Status = status }, cancellationToken: cancellationToken));

        if (order is not null)
        {
            order.Items = new List<OrderItem>();
            _logger.LogInformation("Order {OrderId} status updated to {Status}.", id, status);
        }
        return order;
    }

    // ── Cart ──────────────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<CartItemDb>> GetCartItemsAsync(int userId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, UserId, ProductId, ProductName, ImageUrl, UnitPrice, Quantity, AddedAtUtc
            FROM CartItems
            WHERE UserId = @UserId
            ORDER BY AddedAtUtc;
            """;

        await using var connection = new SqlConnection(_dbOptions.ConnectionString);
        var results = await connection.QueryAsync<CartItemDb>(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken));
        return results.ToList();
    }

    public async Task<CartItemDb?> GetCartItemAsync(int cartItemId, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, UserId, ProductId, ProductName, ImageUrl, UnitPrice, Quantity, AddedAtUtc
            FROM CartItems
            WHERE Id = @Id;
            """;

        await using var connection = new SqlConnection(_dbOptions.ConnectionString);
        return await connection.QuerySingleOrDefaultAsync<CartItemDb>(
            new CommandDefinition(sql, new { Id = cartItemId }, cancellationToken: cancellationToken));
    }

    public async Task<CartItemDb> AddToCartAsync(
        AddToCartRequest request,
        CatalogProductDto product,
        CancellationToken cancellationToken = default)
    {
        // Upsert: increment quantity if the product is already in the cart,
        // otherwise insert a new row.
        const string sql = """
            MERGE INTO CartItems AS target
            USING (SELECT @UserId AS UserId, @ProductId AS ProductId) AS source
                ON target.UserId = source.UserId AND target.ProductId = source.ProductId
            WHEN MATCHED THEN
                UPDATE SET
                    Quantity    = target.Quantity + @Quantity,
                    UnitPrice   = @UnitPrice,
                    ProductName = @ProductName,
                    ImageUrl    = @ImageUrl
            WHEN NOT MATCHED THEN
                INSERT (UserId, ProductId, ProductName, ImageUrl, UnitPrice, Quantity)
                VALUES (@UserId, @ProductId, @ProductName, @ImageUrl, @UnitPrice, @Quantity)
            OUTPUT
                INSERTED.Id, INSERTED.UserId, INSERTED.ProductId, INSERTED.ProductName,
                INSERTED.ImageUrl, INSERTED.UnitPrice, INSERTED.Quantity, INSERTED.AddedAtUtc;
            """;

        await using var connection = new SqlConnection(_dbOptions.ConnectionString);
        var item = await connection.QuerySingleAsync<CartItemDb>(
            new CommandDefinition(sql,
                new
                {
                    request.UserId,
                    request.ProductId,
                    ProductName = product.Name,
                    ImageUrl    = product.ImageUrl ?? string.Empty,
                    UnitPrice   = product.Price,
                    request.Quantity
                },
                cancellationToken: cancellationToken));
        return item;
    }

    public async Task<CartItemDb?> UpdateCartItemAsync(int cartItemId, int quantity, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE CartItems
            SET Quantity = @Quantity
            OUTPUT INSERTED.Id, INSERTED.UserId, INSERTED.ProductId, INSERTED.ProductName,
                   INSERTED.ImageUrl, INSERTED.UnitPrice, INSERTED.Quantity, INSERTED.AddedAtUtc
            WHERE Id = @Id;
            """;

        await using var connection = new SqlConnection(_dbOptions.ConnectionString);
        return await connection.QuerySingleOrDefaultAsync<CartItemDb>(
            new CommandDefinition(sql, new { Id = cartItemId, Quantity = quantity }, cancellationToken: cancellationToken));
    }

    public async Task<bool> DeleteCartItemAsync(int cartItemId, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM CartItems WHERE Id = @Id;";

        await using var connection = new SqlConnection(_dbOptions.ConnectionString);
        var rows = await connection.ExecuteAsync(
            new CommandDefinition(sql, new { Id = cartItemId }, cancellationToken: cancellationToken));
        return rows > 0;
    }

    public async Task ClearCartAsync(int userId, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM CartItems WHERE UserId = @UserId;";

        await using var connection = new SqlConnection(_dbOptions.ConnectionString);
        await connection.ExecuteAsync(
            new CommandDefinition(sql, new { UserId = userId }, cancellationToken: cancellationToken));
        _logger.LogInformation("Cleared cart for user {UserId}.", userId);
    }

    public async Task<CartItemDb?> UpdateCartItemByProductAsync(
        int userId, int productId, int quantity, CancellationToken cancellationToken = default)
    {
        const string sql = """
            UPDATE CartItems
            SET Quantity = @Quantity
            OUTPUT INSERTED.Id, INSERTED.UserId, INSERTED.ProductId, INSERTED.ProductName,
                   INSERTED.ImageUrl, INSERTED.UnitPrice, INSERTED.Quantity, INSERTED.AddedAtUtc
            WHERE UserId = @UserId AND ProductId = @ProductId;
            """;

        await using var connection = new SqlConnection(_dbOptions.ConnectionString);
        return await connection.QuerySingleOrDefaultAsync<CartItemDb>(
            new CommandDefinition(sql,
                new { UserId = userId, ProductId = productId, Quantity = quantity },
                cancellationToken: cancellationToken));
    }

    public async Task<bool> DeleteCartItemByProductAsync(
        int userId, int productId, CancellationToken cancellationToken = default)
    {
        const string sql = "DELETE FROM CartItems WHERE UserId = @UserId AND ProductId = @ProductId;";

        await using var connection = new SqlConnection(_dbOptions.ConnectionString);
        var rows = await connection.ExecuteAsync(
            new CommandDefinition(sql,
                new { UserId = userId, ProductId = productId },
                cancellationToken: cancellationToken));
        return rows > 0;
    }
}
