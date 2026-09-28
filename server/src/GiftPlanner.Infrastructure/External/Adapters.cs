using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GiftPlanner.Application.Common;
using GiftPlanner.Domain.Users;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace GiftPlanner.Infrastructure.External;

public sealed class ClockOptions
{
    public string TimeZone { get; set; } = "Asia/Jerusalem";
}

/// <summary>"Today" is the user's local (Israel) date, not UTC, so "in 12 days" is correct at 01:00.</summary>
internal sealed class SystemClock(IOptions<ClockOptions> options) : IClock
{
    private readonly TimeZoneInfo _zone = TimeZoneInfo.FindSystemTimeZoneById(options.Value.TimeZone);
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    public DateOnly Today => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(UtcNow, _zone).DateTime);
}

/// <summary>JSON cache on IDistributedCache: Redis in production, in-memory when Redis is not configured.</summary>
internal sealed class DistributedCacheService(IDistributedCache cache) : ICacheService
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public async Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan ttl, CancellationToken ct)
    {
        try
        {
            var hit = await cache.GetStringAsync(key, ct);
            if (hit is not null && JsonSerializer.Deserialize<T>(hit, Options) is { } value) return value;
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            // A cache outage must never break the request: fall through to the factory.
        }

        var created = await factory(ct);
        try
        {
            await cache.SetStringAsync(key, JsonSerializer.Serialize(created, Options),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl }, ct);
        }
        catch (Exception) when (!ct.IsCancellationRequested) { }
        return created;
    }

    public Task RemoveAsync(string key, CancellationToken ct) => cache.RemoveAsync(key, ct);
}

public sealed class LlmOptions
{
    public string? ApiKey { get; set; }
    public string BaseUrl { get; set; } = "https://api.anthropic.com/";
    /// <summary>Model ids are configuration, not code: update them in appsettings when newer models ship.</summary>
    public string FastModel { get; set; } = "claude-haiku-4-5";
    public string SmartModel { get; set; } = "claude-sonnet-4-5";
    public int TimeoutSeconds { get; set; } = 12;
}

/// <summary>Anthropic Messages API over plain HttpClient (no SDK dependency). The key lives only on the server.</summary>
internal sealed class AnthropicLlmClient(HttpClient http, IOptions<LlmOptions> options) : ILlmClient
{
    private readonly LlmOptions _o = options.Value;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_o.ApiKey);

    public async Task<LlmResult> CompleteAsync(LlmRequest request, CancellationToken ct)
    {
        if (!IsConfigured) throw new InvalidOperationException("LLM is not configured.");
        var model = request.Tier == LlmTier.Fast ? _o.FastModel : _o.SmartModel;

        using var msg = new HttpRequestMessage(HttpMethod.Post, "v1/messages")
        {
            Content = JsonContent.Create(new
            {
                model,
                max_tokens = request.MaxTokens,
                temperature = request.Temperature,
                system = request.System,
                messages = new[] { new { role = "user", content = request.User } }
            })
        };
        msg.Headers.Add("x-api-key", _o.ApiKey);
        msg.Headers.Add("anthropic-version", "2023-06-01");

        using var res = await http.SendAsync(msg, ct);
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<AnthropicResponse>(cancellationToken: ct)
                   ?? throw new InvalidOperationException("Empty LLM response.");
        var text = string.Concat(body.Content.Where(c => c.Type == "text").Select(c => c.Text));
        return new LlmResult(text, body.Usage?.InputTokens ?? 0, body.Usage?.OutputTokens ?? 0, body.Model ?? model);
    }

    private sealed record AnthropicResponse(
        [property: JsonPropertyName("content")] List<ContentBlock> Content,
        [property: JsonPropertyName("usage")] Usage? Usage,
        [property: JsonPropertyName("model")] string? Model);

    private sealed record ContentBlock([property: JsonPropertyName("type")] string Type, [property: JsonPropertyName("text")] string? Text);

    private sealed record Usage(
        [property: JsonPropertyName("input_tokens")] int InputTokens,
        [property: JsonPropertyName("output_tokens")] int OutputTokens);
}

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "gift-planner";
    public string Audience { get; set; } = "gift-planner-app";
    /// <summary>At least 32 bytes. Comes from a secret store / environment variable, never from source control.</summary>
    public string SigningKey { get; set; } = "";
    public int AnonymousDays { get; set; } = 180;
    public int SignedInDays { get; set; } = 30;
}

internal sealed class JwtTokenIssuer(IOptions<JwtOptions> options, IClock clock) : ITokenIssuer
{
    private readonly JwtOptions _o = options.Value;

    public string Issue(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_o.SigningKey));
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _o.Issuer,
            Audience = _o.Audience,
            Subject = new ClaimsIdentity(
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim("anon", user.IsAnonymous ? "true" : "false")
            ]),
            IssuedAt = clock.UtcNow.UtcDateTime,
            Expires = clock.UtcNow.UtcDateTime.AddDays(user.IsAnonymous ? _o.AnonymousDays : _o.SignedInDays),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };
        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}

public sealed class OidcProviderOptions
{
    public string? GoogleClientId { get; set; }
    public string? AppleClientId { get; set; }
    public bool EnableDevProvider { get; set; }
}

/// <summary>Verifies Google / Apple ID tokens against the provider's published signing keys.</summary>
internal sealed class OidcIdTokenVerifier : IExternalIdentityVerifier
{
    private readonly ConfigurationManager<OpenIdConnectConfiguration> _config;
    private readonly string _issuer;
    private readonly string? _audience;

    public string Provider { get; }

    public OidcIdTokenVerifier(string provider, string authority, string? audience)
    {
        Provider = provider;
        _issuer = authority;
        _audience = audience;
        _config = new ConfigurationManager<OpenIdConnectConfiguration>(
            $"{authority.TrimEnd('/')}/.well-known/openid-configuration", new OpenIdConnectConfigurationRetriever());
    }

    public async Task<ExternalIdentity?> VerifyAsync(string idToken, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(_audience)) return null; // provider not configured
        var oidc = await _config.GetConfigurationAsync(ct);
        var result = await new JsonWebTokenHandler().ValidateTokenAsync(idToken, new TokenValidationParameters
        {
            ValidIssuers = [_issuer, _issuer.Replace("https://", "")],
            ValidAudience = _audience,
            IssuerSigningKeys = oidc.SigningKeys,
            ValidateLifetime = true
        });
        if (!result.IsValid) return null;
        var sub = result.ClaimsIdentity.FindFirst("sub")?.Value;
        var name = result.ClaimsIdentity.FindFirst("given_name")?.Value ?? result.ClaimsIdentity.FindFirst("name")?.Value;
        return sub is null ? null : new ExternalIdentity(Provider, sub, name);
    }
}

/// <summary>Development-only provider: "dev:<id>|<name>". Registered only when explicitly enabled.</summary>
internal sealed class DevIdentityVerifier : IExternalIdentityVerifier
{
    public string Provider => "dev";

    public Task<ExternalIdentity?> VerifyAsync(string idToken, CancellationToken ct)
    {
        if (!idToken.StartsWith("dev:", StringComparison.Ordinal)) return Task.FromResult<ExternalIdentity?>(null);
        var parts = idToken[4..].Split('|', 2);
        return Task.FromResult<ExternalIdentity?>(new ExternalIdentity("dev", parts[0], parts.Length > 1 ? parts[1] : null));
    }
}
