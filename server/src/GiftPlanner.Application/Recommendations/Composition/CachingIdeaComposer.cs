using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GiftPlanner.Application.Common;

namespace GiftPlanner.Application.Recommendations.Composition;

/// <summary>
/// Decorator: identical brief + identical top ideas → reuse the LLM copy (going back, refreshing, sharing).
/// Only LLM results are cached; template output is free and a temporary LLM outage should not be cached for a day.
/// </summary>
public sealed class CachingIdeaComposer(IIdeaComposer inner, ICacheService cache, TimeSpan ttl) : IIdeaComposer
{
    public const string PromptVersion = "v1";

    public string Name => inner.Name;

    public async Task<ComposeResult> ComposeAsync(ComposeRequest request, CancellationToken ct)
    {
        var key = Fingerprint(request);
        ComposeResult? fresh = null;
        var cached = await cache.GetOrCreateAsync(key, async c =>
        {
            fresh = await inner.ComposeAsync(request, c);
            return fresh;
        }, ttl, ct);

        if (fresh is not null && !fresh.ComposerName.StartsWith("llm", StringComparison.Ordinal))
            await cache.RemoveAsync(key, ct);

        // A cache hit costs no tokens.
        return fresh ?? cached with { TokensIn = 0, TokensOut = 0 };
    }

    internal static string Fingerprint(ComposeRequest r)
    {
        var payload = JsonSerializer.Serialize(new
        {
            v = PromptVersion,
            r.Brief,
            ideas = r.Ideas.Select(i => i.Idea.Id)
        });
        return "compose:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(payload)))[..32];
    }
}
