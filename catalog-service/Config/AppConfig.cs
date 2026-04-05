namespace CatalogService.Config;

/// <summary>
/// All runtime configuration loaded exclusively from environment variables.
/// Call <see cref="Load"/> once at startup; it throws <see cref="InvalidOperationException"/>
/// with a descriptive message for every missing required variable so the pod
/// fails fast rather than crashing inside a request handler later.
/// </summary>
public sealed class AppConfig
{
    // ── DB ──────────────────────────────────────────────────────────────────
    /// <summary>Resolved DB name — used in startup log messages.</summary>
    public string DbName           { get; }
    public string ConnectionString { get; }

    private AppConfig(string dbName, string connectionString)
    {
        DbName           = dbName;
        ConnectionString = connectionString;
    }

    public static AppConfig Load()
    {
        var dbHost     = Require("DB_HOST");
        var dbName     = Optional("DB_NAME", "KubeCart_Catalog");
        var dbUser     = Require("DB_USER");
        var dbPassword = Require("DB_PASSWORD");

        var connectionString =
            $"Server={dbHost};Database={dbName};" +
            $"User Id={dbUser};Password={dbPassword};" +
            "TrustServerCertificate=True;Encrypt=False;";

        return new AppConfig(dbName, connectionString);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────
    private static string Require(string key) =>
        Environment.GetEnvironmentVariable(key)
        ?? throw new InvalidOperationException(
            $"Required environment variable '{key}' is not set. " +
            $"Set it via a Kubernetes Secret/ConfigMap or a local .env file.");

    private static string Optional(string key, string defaultValue) =>
        Environment.GetEnvironmentVariable(key) ?? defaultValue;
}
