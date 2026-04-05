using Dapper;
using Microsoft.Data.SqlClient;
using IdentityService.Models;

namespace IdentityService.Data;

public sealed class UserRepository : IUserRepository
{
    private readonly DbOptions _dbOptions;
    private readonly ILogger<UserRepository> _logger;

    public UserRepository(DbOptions dbOptions, ILogger<UserRepository> logger)
    {
        _dbOptions = dbOptions;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
            BEGIN
                CREATE TABLE Users (
                    Id           INT IDENTITY(1,1) PRIMARY KEY,
                    Email        NVARCHAR(255) NOT NULL UNIQUE,
                    PasswordHash NVARCHAR(500) NOT NULL,
                    FullName     NVARCHAR(200) NOT NULL,
                    CreatedAtUtc DATETIME2     NOT NULL CONSTRAINT DF_Users_CreatedAtUtc DEFAULT(SYSUTCDATETIME())
                );
            END
            """;

        await using var connection = new SqlConnection(_dbOptions.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(sql, cancellationToken: cancellationToken));
        _logger.LogInformation("Ensured Users table exists.");
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, Email, PasswordHash, FullName, CreatedAtUtc
            FROM Users WHERE Email = @Email;
            """;

        await using var connection = new SqlConnection(_dbOptions.ConnectionString);
        return await connection.QuerySingleOrDefaultAsync<User>(
            new CommandDefinition(sql, new { Email = email }, cancellationToken: cancellationToken));
    }

    public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT Id, Email, PasswordHash, FullName, CreatedAtUtc
            FROM Users WHERE Id = @Id;
            """;

        await using var connection = new SqlConnection(_dbOptions.ConnectionString);
        return await connection.QuerySingleOrDefaultAsync<User>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
    }

    public async Task<User> CreateAsync(string email, string passwordHash, string fullName, CancellationToken cancellationToken = default)
    {
        const string sql = """
            INSERT INTO Users (Email, PasswordHash, FullName)
            OUTPUT INSERTED.Id, INSERTED.Email, INSERTED.PasswordHash, INSERTED.FullName, INSERTED.CreatedAtUtc
            VALUES (@Email, @PasswordHash, @FullName);
            """;

        await using var connection = new SqlConnection(_dbOptions.ConnectionString);
        var user = await connection.QuerySingleAsync<User>(
            new CommandDefinition(sql,
                new { Email = email, PasswordHash = passwordHash, FullName = fullName },
                cancellationToken: cancellationToken));

        _logger.LogInformation("Created user {UserId} ({Email}).", user.Id, user.Email);
        return user;
    }
}
