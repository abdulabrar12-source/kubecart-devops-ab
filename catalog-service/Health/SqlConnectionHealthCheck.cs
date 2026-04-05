using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using CatalogService.Data;

namespace CatalogService.Health;

public sealed class SqlConnectionHealthCheck : IHealthCheck
{
    private readonly DbOptions _dbOptions;

    public SqlConnectionHealthCheck(DbOptions dbOptions)
    {
        _dbOptions = dbOptions;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new SqlConnection(_dbOptions.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            _ = await connection.ExecuteScalarAsync<int>(
                new CommandDefinition("SELECT 1", cancellationToken: cancellationToken));
            return HealthCheckResult.Healthy("SQL Server is reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("SQL Server health check failed.", ex);
        }
    }
}
