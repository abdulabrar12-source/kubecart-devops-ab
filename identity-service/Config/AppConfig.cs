namespace IdentityService.Config;

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

    // ── JWT ─────────────────────────────────────────────────────────────────
    /// <summary>HMAC-SHA256 signing key for JWT tokens (env: JWT_SIGNING_KEY).</summary>
    public string JwtSigningKey    { get; }

    // ── Encryption ──────────────────────────────────────────────────────────
    /// <summary>AES-256 key for encrypting PII at rest (env: APP_ENCRYPTION_KEY).</summary>
    public string AppEncryptionKey { get; }

    private AppConfig(
        string dbName,
        string connectionString,
        string jwtSigningKey,
        string appEncryptionKey)
    {
        DbName           = dbName;
        ConnectionString = connectionString;
        JwtSigningKey    = jwtSigningKey;
        AppEncryptionKey = appEncryptionKey;
    }

    public static AppConfig Load()
    {
        var dbHost     = Require("DB_HOST");
        var dbName     = Optional("DB_NAME", "KubeCart_Identity");
        var dbUser     = Require("DB_USER");
        var dbPassword = Require("DB_PASSWORD");

        var connectionString =
            $"Server={dbHost};Database={dbName};" +
            $"User Id={dbUser};Password={dbPassword};" +
            "TrustServerCertificate=True;Encrypt=False;";

        var jwtSigningKey    = Require("JWT_SIGNING_KEY");
        var appEncryptionKey = Require("APP_ENCRYPTION_KEY");

        return new AppConfig(dbName, connectionString, jwtSigningKey, appEncryptionKey);
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
