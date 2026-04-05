using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using IdentityService.Config;
using IdentityService.Data;
using IdentityService.Health;
using IdentityService.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// ── Configuration (all values from environment variables) ────────────────────
var config      = AppConfig.Load();
var environment = builder.Environment.EnvironmentName;

builder.Services.AddSingleton(new DbOptions { ConnectionString = config.ConnectionString });
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddSingleton<PasswordHasher<string>>();
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
        var repository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        await repository.InitializeAsync();
        startupLogger.LogInformation("Identity service initialized successfully.");
    }
    catch (Exception ex)
    {
        startupLogger.LogError(ex, "Failed to initialize identity service.");
        throw;
    }
}

app.UseCors();

// ── JWT helper ─────────────────────────────────────────────────────────────────
string GenerateToken(User user)
{
    var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config.JwtSigningKey));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
    var claims = new[]
    {
        new Claim(JwtRegisteredClaimNames.Sub,   user.Id.ToString()),
        new Claim(JwtRegisteredClaimNames.Email, user.Email),
        new Claim("fullName",                    user.FullName),
        new Claim(JwtRegisteredClaimNames.Jti,   Guid.NewGuid().ToString()),
    };
    var token = new JwtSecurityToken(
        issuer:            "identity-service",
        audience:          "kubecart",
        claims:            claims,
        expires:           DateTime.UtcNow.AddDays(7),
        signingCredentials: creds);
    return new JwtSecurityTokenHandler().WriteToken(token);
}

// ── Routes ─────────────────────────────────────────────────────────────────────
app.MapGet("/", () => Results.Ok(new { message = "Identity Service is running" }));

app.MapPost("/api/auth/register", async (
    RegisterRequest request,
    IUserRepository repo,
    PasswordHasher<string> hasher,
    ILoggerFactory loggerFactory,
    CancellationToken cancellationToken) =>
{
    var logger = loggerFactory.CreateLogger("AuthEndpoints");

    if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        return Results.BadRequest(new { error = "Email and Password are required." });

    if (string.IsNullOrWhiteSpace(request.FullName))
        return Results.BadRequest(new { error = "Full name is required." });

    var normalizedEmail = request.Email.Trim().ToLower();
    var existing = await repo.GetByEmailAsync(normalizedEmail, cancellationToken);
    if (existing is not null)
        return Results.Conflict(new { error = "Email is already registered." });

    var hash = hasher.HashPassword(normalizedEmail, request.Password);
    var user = await repo.CreateAsync(normalizedEmail, hash, request.FullName.Trim(), cancellationToken);

    logger.LogInformation("Registered user {UserId} ({Email}).", user.Id, user.Email);
    return Results.Created("/api/auth/me", new AuthResponse
    {
        UserId   = user.Id,
        Email    = user.Email,
        FullName = user.FullName,
        Token    = GenerateToken(user),
    });
});

app.MapPost("/api/auth/login", async (
    LoginRequest request,
    IUserRepository repo,
    PasswordHasher<string> hasher,
    ILoggerFactory loggerFactory,
    CancellationToken cancellationToken) =>
{
    var logger = loggerFactory.CreateLogger("AuthEndpoints");

    if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        return Results.BadRequest(new { error = "Email and Password are required." });

    var normalizedEmail = request.Email.Trim().ToLower();
    var user = await repo.GetByEmailAsync(normalizedEmail, cancellationToken);
    if (user is null)
        return Results.Unauthorized();

    var result = hasher.VerifyHashedPassword(normalizedEmail, user.PasswordHash, request.Password);
    if (result == PasswordVerificationResult.Failed)
    {
        logger.LogWarning("Invalid password attempt for {Email}.", normalizedEmail);
        return Results.Unauthorized();
    }

    logger.LogInformation("User {UserId} ({Email}) logged in.", user.Id, user.Email);
    return Results.Ok(new AuthResponse
    {
        UserId   = user.Id,
        Email    = user.Email,
        FullName = user.FullName,
        Token    = GenerateToken(user),
    });
});

app.MapGet("/api/auth/me", async (
    HttpContext httpContext,
    IUserRepository repo,
    CancellationToken cancellationToken) =>
{
    var authHeader = httpContext.Request.Headers["Authorization"].FirstOrDefault();
    if (authHeader is null || !authHeader.StartsWith("Bearer "))
        return Results.Unauthorized();

    var token = authHeader["Bearer ".Length..].Trim();
    try
    {
        var handler = new JwtSecurityTokenHandler();
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config.JwtSigningKey));
        handler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = key,
            ValidateIssuer           = true,
            ValidIssuer              = "identity-service",
            ValidateAudience         = true,
            ValidAudience            = "kubecart",
            ValidateLifetime         = true,
            ClockSkew                = TimeSpan.Zero,
        }, out _);

        var jwt    = handler.ReadJwtToken(token);
        var userId = int.Parse(jwt.Subject);
        var user   = await repo.GetByIdAsync(userId, cancellationToken);

        if (user is null) return Results.Unauthorized();
        return Results.Ok(new { userId = user.Id, email = user.Email, fullName = user.FullName });
    }
    catch
    {
        return Results.Unauthorized();
    }
});

app.MapHealthChecks("/health", new HealthCheckOptions());

app.Run();
