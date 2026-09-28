using GiftPlanner.Domain.Catalog;
using GiftPlanner.Domain.Common;

namespace GiftPlanner.Application.Recommendations.Scoring;

/// <summary>
/// One explainable dimension of "how good is this idea for this person", returning 0..1.
/// New dimensions are added as new classes (Open/Closed); the scorer does not change.
/// </summary>
public interface IScoreComponent
{
    string Name { get; }
    double Score(CatalogIdea idea, ScoringContext ctx);
}

public sealed class InterestScore : IScoreComponent
{
    public string Name => ScoreNames.Interests;

    public double Score(CatalogIdea idea, ScoringContext ctx)
    {
        if (ctx.Brief.Interests.Count == 0 || idea.Interests.Count == 0) return 0.3; // neutral when we know nothing
        var matches = idea.Interests.Count(i => ctx.Brief.Interests.Contains(i));
        if (matches == 0) return 0;
        // Two matching interests is already a strong signal; more is a bonus, not a requirement.
        return Math.Min(1.0, 0.6 + 0.2 * (matches - 1));
    }
}

public sealed class EmotionScore : IScoreComponent
{
    public string Name => ScoreNames.Emotion;

    public double Score(CatalogIdea idea, ScoringContext ctx)
    {
        if (ctx.Brief.Emotions.Count == 0) return 0.5;
        var matches = ctx.Brief.Emotions.Count(e => idea.Emotions.Contains(e));
        var baseScore = (double)matches / ctx.Brief.Emotions.Count;
        // "Surprised" is also satisfied by a genuinely surprising idea even if it is not tagged.
        if (ctx.Brief.Emotions.Contains(Emotion.Surprised) && idea.Surprise >= 4)
            baseScore = Math.Max(baseScore, 0.5 + 0.5 * baseScore);
        return baseScore;
    }
}

public sealed class BudgetScore : IScoreComponent
{
    public string Name => ScoreNames.Budget;

    /// <summary>1.0 when the typical price uses 60–100% of the budget; lower when far below or above.</summary>
    public double Score(CatalogIdea idea, ScoringContext ctx)
    {
        var budget = ctx.BudgetCap;
        if (budget <= 0) return idea.PriceMin == 0 ? 1 : 0.5;
        var typical = (idea.PriceMin + idea.PriceMax) / 2.0;
        var ratio = typical / budget;
        return ratio switch
        {
            >= 0.6 and <= 1.0 => 1.0,
            < 0.6 => Math.Max(0.3, ratio / 0.6),
            _ => Math.Max(0, 1 - (ratio - 1) / 0.3)
        };
    }
}

public sealed class OccasionScore : IScoreComponent
{
    public string Name => ScoreNames.Occasion;
    public double Score(CatalogIdea idea, ScoringContext ctx) =>
        idea.Occasions.Count == 0 ? 0.6 : idea.Occasions.Contains(ctx.Brief.OccasionKey) ? 1.0 : 0.2;
}

public sealed class RelationshipScore : IScoreComponent
{
    public string Name => ScoreNames.Relationship;
    public double Score(CatalogIdea idea, ScoringContext ctx) =>
        idea.Relationships.Count == 0 ? 0.6 : idea.Relationships.Contains(ctx.Brief.RelationshipKey) ? 1.0 : 0.1;
}

public sealed class NoveltyScore : IScoreComponent
{
    public string Name => ScoreNames.Novelty;
    public double Score(CatalogIdea idea, ScoringContext ctx)
    {
        var novelty = (idea.Novelty - 1) / 4.0;
        return ctx.PreferSurprise ? Math.Min(1, 0.5 * novelty + 0.5 * (idea.Surprise - 1) / 4.0 + 0.2) : novelty;
    }
}

public sealed class AvailabilityScore : IScoreComponent
{
    public string Name => ScoreNames.Availability;

    /// <summary>Full score when there is comfortable lead time; drops as the deadline gets tight.</summary>
    public double Score(CatalogIdea idea, ScoringContext ctx)
    {
        if (ctx.DaysUntilEvent is not { } days || days < 0) return 1;
        var slack = days - idea.LeadTimeDays;
        return slack >= 3 ? 1 : slack >= 0 ? 0.6 : 0;
    }
}
