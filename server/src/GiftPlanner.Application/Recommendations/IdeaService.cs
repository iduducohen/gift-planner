using System.Diagnostics;
using GiftPlanner.Application.Common;
using GiftPlanner.Application.Contracts;
using GiftPlanner.Application.Recommendations.Composition;
using GiftPlanner.Application.Recommendations.Refine;
using GiftPlanner.Application.Recommendations.Scoring;
using GiftPlanner.Application.Taxonomy;
using GiftPlanner.Domain.Catalog;
using GiftPlanner.Domain.Common;
using GiftPlanner.Domain.GiftSessions;
using GiftPlanner.Domain.Ideas;

namespace GiftPlanner.Application.Recommendations;

public interface IIdeaService
{
    Task<Result<IdeaSetDto>> GenerateAsync(Guid sessionId, GenerateIdeasRequest request, CancellationToken ct);
    Task<Result<IdeaPageDto>> GetPageAsync(Guid setId, string? cursor, int limit, string? filter, CancellationToken ct);
    Task<Result<IdeaDetailDto>> GetDetailAsync(Guid recommendationId, CancellationToken ct);
    IReadOnlyList<RefineOptionDto> GetRefineOptions();
}

/// <summary>Resolves the best partner offer for each idea across all registered providers.</summary>
public interface IOfferResolver
{
    Task<IReadOnlyDictionary<Guid, OfferQuote>> BestOffersAsync(IReadOnlyCollection<Guid> ideaIds, CancellationToken ct);
}

public sealed class OfferResolver(IEnumerable<IOfferProvider> providers) : IOfferResolver
{
    public async Task<IReadOnlyDictionary<Guid, OfferQuote>> BestOffersAsync(IReadOnlyCollection<Guid> ideaIds, CancellationToken ct)
    {
        var all = new List<OfferQuote>();
        foreach (var p in providers)
            all.AddRange(await p.QuoteAsync(ideaIds, ct));
        // Cheapest verified offer wins. Sponsorship is deliberately NOT a factor.
        return all
            .Where(q => q.PartnerVerified)
            .GroupBy(q => q.IdeaId)
            .ToDictionary(g => g.Key, g => g.OrderBy(q => q.FinalPrice).First());
    }
}

/// <summary>
/// The core use case: brief → filtered, scored, diversified catalog ideas → copy for the top 5 →
/// one stored IdeaSet. The LLM writes words for 5 ideas; everything else is deterministic code.
/// </summary>
public sealed class IdeaService(
    IGiftSessionRepository sessions,
    IRecipientRepository recipients,
    IIdeaSetRepository sets,
    ISavedIdeaRepository saved,
    ICatalogReader catalog,
    IOfferResolver offers,
    IRecommendationScorer scorer,
    IDiversityReranker reranker,
    IRefineStrategyResolver refine,
    IRefineTextInterpreter refineInterpreter,
    IIdeaComposer composer,
    ITaxonomyService taxonomy,
    ICurrentUser user,
    IClock clock,
    IUnitOfWork uow) : IIdeaService
{
    public const int TopCount = 5;
    public const int MaxIdeas = 29;
    public const int DefaultPageSize = 8;
    public const int MaxPageSize = 24;

    public async Task<Result<IdeaSetDto>> GenerateAsync(Guid sessionId, GenerateIdeasRequest request, CancellationToken ct)
    {
        using var activity = Telemetry.Source.StartActivity("ideas.generate");
        var started = Stopwatch.GetTimestamp();

        var session = await sessions.GetAsync(sessionId, user.UserId, ct);
        if (session is null) return Error.NotFound("Session");
        if (session.Status == GiftSessionStatus.AwaitingClarification)
            return Error.Conflict("Answer or skip the clarifying question first.");

        var brief = session.Brief;
        var history = session.RecipientId is { } rid && await recipients.GetAsync(rid, user.UserId, ct) is { } recipient
            ? recipient.History.Select(h => h.Title).ToList()
            : [];

        // Free text refinement is interpreted into a preset + extra "avoid" terms.
        var presetKey = request.RefinePreset;
        if (!string.IsNullOrWhiteSpace(request.RefineText))
        {
            var intent = await refineInterpreter.InterpretAsync(request.RefineText.Trim(), ct);
            presetKey ??= intent.PresetKey;
            brief = brief with { Avoid = brief.Avoid.Concat(intent.ExtraAvoid).Distinct().ToList() };
            if (intent.ExtraInterests.Count > 0) brief = brief.WithInterests(intent.ExtraInterests);
        }

        var strategy = refine.Find(presetKey);
        if (presetKey is not null && strategy is null) return Error.Validation($"Unknown refine preset '{presetKey}'.");

        var audience = taxonomy.ResolveAudience(brief.RelationshipKey, brief.AgeRangeKey);
        var ctx = ScoringContext.Create(brief, audience, clock.Today, history);
        if (strategy is not null) ctx = strategy.Apply(ctx);

        var ranked = scorer.Rank(await catalog.GetActiveIdeasAsync(ct), ctx);
        if (ranked.Count == 0)
        {
            Telemetry.EmptyResults.Add(1);
            return new Error("no_ideas", "לא מצאנו רעיונות שמתאימים לכל התנאים. נסו להרחיב תקציב או להסיר הגבלה.", ErrorType.Validation);
        }

        var chosen = reranker.Rerank(ranked, TopCount).Take(MaxIdeas).ToList();
        var top = chosen.Take(TopCount).ToList();
        var bestOffers = await offers.BestOffersAsync(chosen.Select(s => s.Idea.Id).ToList(), ct);

        var composeRequest = new ComposeRequest(brief, taxonomy.ResolveGender(brief.RelationshipKey),
            taxonomy.RelationshipLabel(brief.RelationshipKey), top);
        var composed = await composer.ComposeAsync(composeRequest, ct);
        var copyById = composed.Ideas.ToDictionary(c => c.CatalogIdeaId);
        var aiWritten = composed.ComposerName.StartsWith("llm", StringComparison.Ordinal);

        var items = chosen.Select((s, i) =>
        {
            var isTop = i < TopCount;
            var copy = isTop && copyById.TryGetValue(s.Idea.Id, out var c) ? c : TemplateIdeaComposer.Compose(s, composeRequest);
            bestOffers.TryGetValue(s.Idea.Id, out var offer);
            return BuildRecommendation(s, copy, offer, isTop, isTop && aiWritten);
        }).ToList();

        var set = IdeaSet.Create(session.Id, user.UserId, request.ParentSetId, presetKey, request.RefineText,
            items, composed.ComposerName, composed.TokensIn, composed.TokensOut, clock.UtcNow);
        sets.Add(set);
        await uow.SaveChangesAsync(ct);

        Telemetry.GenerateLatencyMs.Record(Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        activity?.SetTag("ideas.count", items.Count);
        activity?.SetTag("ideas.composer", composed.ComposerName);

        var savedIds = await SavedIdsAsync(ct);
        var topDtos = set.Items.Where(r => r.IsTop).Select(r => r.ToDto(savedIds.Contains(r.Id))).ToList();
        var more = Page(set, 0, DefaultPageSize, null, savedIds);
        return new IdeaSetDto(set.Id, session.Id, strategy?.Label ?? request.RefineText, set.Items.Count, topDtos, more);
    }

    public async Task<Result<IdeaPageDto>> GetPageAsync(Guid setId, string? cursor, int limit, string? filter, CancellationToken ct)
    {
        var set = await sets.GetAsync(setId, user.UserId, ct);
        if (set is null) return Error.NotFound("Idea set");
        if (!TryDecodeCursor(cursor, out var offset)) return Error.Validation("Invalid cursor.");
        return Page(set, offset, Math.Clamp(limit <= 0 ? DefaultPageSize : limit, 1, MaxPageSize), filter, await SavedIdsAsync(ct));
    }

    public async Task<Result<IdeaDetailDto>> GetDetailAsync(Guid recommendationId, CancellationToken ct)
    {
        var rec = await sets.GetRecommendationAsync(recommendationId, user.UserId, ct);
        if (rec is null) return Error.NotFound("Idea");

        var idea = (await catalog.GetActiveIdeasAsync(ct)).FirstOrDefault(i => i.Id == rec.CatalogIdeaId);
        var quote = (await offers.BestOffersAsync([rec.CatalogIdeaId], ct)).GetValueOrDefault(rec.CatalogIdeaId);
        var offerDto = quote is null
            ? null
            : new OfferDto(quote.OfferId, quote.PartnerName, quote.PartnerRegion, quote.PartnerVerified, quote.ListPrice,
                quote.DiscountPercent, quote.FinalPrice, Mapping.SourceKey(quote.PriceSource), quote.VerifiedAt);
        var isSaved = await saved.FindAsync(user.UserId, rec.Id, ct) is not null;
        return new IdeaDetailDto(rec.ToDto(isSaved), offerDto, idea?.Includes ?? [], idea?.VerifiedAt);
    }

    public IReadOnlyList<RefineOptionDto> GetRefineOptions() => refine.All.Select(s => new RefineOptionDto(s.Key, s.Label)).ToList();

    private static Recommendation BuildRecommendation(ScoredIdea s, ComposedIdea copy, OfferQuote? offer, bool isTop, bool aiWritten)
    {
        var idea = s.Idea;
        return offer is null
            ? Recommendation.Create(idea.Id, isTop, idea.Title, idea.Kind, copy.Why, copy.EmotionalValue, copy.HowTo, idea.Upgrade,
                idea.PriceMin, idea.PriceMax, null, null, null, PriceSource.CatalogEstimate, s.Total, s.Breakdown.ToDictionary(),
                idea.Surprise, idea.Effort, idea.IconKey, aiWritten)
            : Recommendation.Create(idea.Id, isTop, idea.Title, idea.Kind, copy.Why, copy.EmotionalValue, copy.HowTo, idea.Upgrade,
                offer.FinalPrice, offer.FinalPrice, offer.ListPrice, offer.DiscountPercent, offer.OfferId, offer.PriceSource,
                s.Total, s.Breakdown.ToDictionary(), idea.Surprise, idea.Effort, idea.IconKey, aiWritten);
    }

    private static IdeaPageDto Page(IdeaSet set, int offset, int limit, string? filter, IReadOnlySet<Guid> savedIds)
    {
        var rest = set.Items.Where(r => !r.IsTop && Matches(r, filter)).OrderBy(r => r.Rank).ToList();
        var page = rest.Skip(offset).Take(limit).Select(r => r.ToDto(savedIds.Contains(r.Id))).ToList();
        var next = offset + page.Count < rest.Count ? (offset + page.Count).ToString() : null;
        return new IdeaPageDto(page, next, rest.Count);
    }

    private static bool Matches(Recommendation r, string? filter) => filter switch
    {
        null or "" or "all" => true,
        "deal" => r.DiscountPercent is > 0,
        _ => Mapping.KindKey(r.Kind) == filter
    };

    private static bool TryDecodeCursor(string? cursor, out int offset)
    {
        offset = 0;
        return string.IsNullOrEmpty(cursor) || (int.TryParse(cursor, out offset) && offset >= 0);
    }

    private async Task<IReadOnlySet<Guid>> SavedIdsAsync(CancellationToken ct) =>
        (await saved.ListAsync(user.UserId, ct)).Select(s => s.RecommendationId).ToHashSet();
}
