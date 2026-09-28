using System.Text;
using System.Threading.RateLimiting;
using GiftPlanner.Api.Endpoints;
using GiftPlanner.Api.Infrastructure;
using GiftPlanner.Application.Common;
using GiftPlanner.Infrastructure;
using GiftPlanner.Infrastructure.External;
using GiftPlanner.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

// ---- Fail fast on missing secrets (never ship a default signing key) ----
var jwt = config.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
if (Encoding.UTF8.GetByteCount(jwt.SigningKey) < 32)
    throw new InvalidOperationException("Jwt:SigningKey must be set (≥32 bytes) via environment variable Jwt__SigningKey or user-secrets.");

// ---- Layers ----
builder.Services.AddInfrastructure(config);
builder.Services.AddApplication(config);

// ---- HTTP ----
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddOutputCache(o => o.AddPolicy("taxonomy", p => p.Expire(TimeSpan.FromHours(1))));
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase);

builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins(config.GetSection("Cors:Origins").Get<string[]>() ?? [])
    .AllowAnyHeader()
    .AllowAnyMethod()));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false; // keep "sub" / "anon" as issued
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();

// Rate limits per user (falls back to IP). The LLM route is the expensive one; anonymous users get less.
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy(ApiEndpoints.LlmPolicy, http =>
    {
        var userId = HttpCurrentUser.TryGetUserId(http.User)?.ToString();
        var anonymous = http.User.FindFirst("anon")?.Value != "false";
        return RateLimitPartition.GetFixedWindowLimiter(userId ?? http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = anonymous ? 10 : 30, Window = TimeSpan.FromMinutes(10) });
    });
    o.AddPolicy("auth", http => RateLimitPartition.GetFixedWindowLimiter(
        http.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1) }));
});

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("postgres");

// ---- Observability (exported only when an OTLP endpoint is configured) ----
var otlp = config["OpenTelemetry:OtlpEndpoint"];
if (!string.IsNullOrWhiteSpace(otlp))
{
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(r => r.AddService("gift-planner-api"))
        .WithTracing(t => t
            .AddSource(Telemetry.Name)
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(e => e.Endpoint = new Uri(otlp)))
        .WithMetrics(m => m
            .AddMeter(Telemetry.Name)
            .AddAspNetCoreInstrumentation()
            .AddOtlpExporter(e => e.Endpoint = new Uri(otlp)));
}

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();
if (!app.Environment.IsDevelopment()) app.UseHsts();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.UseOutputCache();

if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.MapHealthChecks("/health");
app.MapApi();

await app.Services.InitializeDatabaseAsync();
app.Run();

public partial class Program;
