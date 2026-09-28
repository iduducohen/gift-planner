using GiftPlanner.Domain.Catalog;
using GiftPlanner.Domain.Common;

namespace GiftPlanner.Application.Recommendations.Scoring;

public sealed record ScoredIdea(CatalogIdea Idea, double Total, IReadOnlyDictionary<string, double> Breakdown);

public interface IRecommendationScorer
{
    IReadOnlyList<ScoredIdea> Rank(IEnumerable<CatalogIdea> ideas, ScoringContext ctx);
}

/// <summary>
/// Filter, then score with a weighted sum of components. Deterministic, explainable, cheap.
/// Every score keeps its breakdown so we can debug "why was this suggested" and tune weights later.
/// </summary>
public sealed class RecommendationScorer(IEnumerable<IHardFilter> filters, IEnumerable<IScoreComponent> components)
    : IRecommendationScorer
{
    private readonly IReadOnlyList<IHardFilter> _filters = filters.ToList();
    private readonly IReadOnlyList<IScoreComponent> _components = components.ToList();

    public IReadOnlyList<ScoredIdea> Rank(IEnumerable<CatalogIdea> ideas, ScoringContext ctx) =>
        ideas
            .Where(i => i.IsActive && _filters.All(f => f.Allows(i, ctx)))
            .Select(i => Score(i, ctx))
            .OrderByDescending(s => s.Total)
            .ThenBy(s => s.Idea.PriceMin)
            .ToList();

    private ScoredIdea Score(CatalogIdea idea, ScoringContext ctx)
    {
        var breakdown = _components.ToDictionary(c => c.Name, c => Math.Round(c.Score(idea, ctx), 4));
        var weightSum = _components.Sum(c => ctx.Weights.For(c.Name));
        var total = weightSum <= 0 ? 0 : _components.Sum(c => ctx.Weights.For(c.Name) * breakdown[c.Name]) / weightSum;
        if (ctx.KindBoost is not null && ctx.KindBoost.TryGetValue(idea.Kind, out var boost))
            total *= boost;
        return new ScoredIdea(idea, Math.Round(total, 4), breakdown);
    }
}

public interface IDiversityReranker
{
    IReadOnlyList<ScoredIdea> Rerank(IReadOnlyList<ScoredIdea> ranked, int topCount);
}

/// <summary>
/// Makes the top N varied: at most two ideas of the same kind, and at least one high-surprise idea
/// (the "something unexpected" slot). Everything else keeps its score order.
/// </summary>
public sealed class DiversityReranker : IDiversityReranker
{
    public const int MaxPerKind = 2;

    public IReadOnlyList<ScoredIdea> Rerank(IReadOnlyList<ScoredIdea> ranked, int topCount)
    {
        var top = new List<ScoredIdea>();
        var perKind = new Dictionary<IdeaKind, int>();

        foreach (var s in ranked)
        {
            if (top.Count == topCount) break;
            perKind.TryGetValue(s.Idea.Kind, out var n);
            if (n >= MaxPerKind) continue;
            top.Add(s);
            perKind[s.Idea.Kind] = n + 1;
        }

        // Not enough variety in the pool: fill up with the best remaining regardless of kind.
        foreach (var s in ranked)
        {
            if (top.Count == topCount) break;
            if (!top.Contains(s)) top.Add(s);
        }

        if (top.Count > 0 && top.All(s => s.Idea.Surprise < 4))
        {
            var surprising = ranked.FirstOrDefault(s => s.Idea.Surprise >= 4 && !top.Contains(s));
            if (surprising is not null) top[^1] = surprising;
        }

        var rest = ranked.Where(s => !top.Contains(s));
        return top.Concat(rest).ToList();
    }
}
