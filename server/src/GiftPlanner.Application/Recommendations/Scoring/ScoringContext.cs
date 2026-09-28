using GiftPlanner.Domain.Common;
using GiftPlanner.Domain.GiftSessions;

namespace GiftPlanner.Application.Recommendations.Scoring;

/// <summary>Relative importance of each score component. Tuned by config and by refine strategies, never by code changes.</summary>
public sealed record ScoringWeights(
    double Interests = 0.25,
    double Emotion = 0.20,
    double Budget = 0.15,
    double Occasion = 0.10,
    double Relationship = 0.10,
    double Novelty = 0.10,
    double Availability = 0.10)
{
    public double For(string component) => component switch
    {
        ScoreNames.Interests => Interests,
        ScoreNames.Emotion => Emotion,
        ScoreNames.Budget => Budget,
        ScoreNames.Occasion => Occasion,
        ScoreNames.Relationship => Relationship,
        ScoreNames.Novelty => Novelty,
        ScoreNames.Availability => Availability,
        _ => 0
    };
}

public static class ScoreNames
{
    public const string Interests = "interests";
    public const string Emotion = "emotion";
    public const string Budget = "budget";
    public const string Occasion = "occasion";
    public const string Relationship = "relationship";
    public const string Novelty = "novelty";
    public const string Availability = "availability";
}

/// <summary>Everything the engine needs to rank ideas for one brief. Immutable; refine strategies return modified copies.</summary>
public sealed record ScoringContext(
    GiftBrief Brief,
    Audience Audience,
    DateOnly Today,
    IReadOnlyCollection<string> PreviousGiftTitles,
    ScoringWeights Weights,
    int BudgetCap,
    IReadOnlySet<IdeaKind>? AllowedKinds = null,
    IReadOnlyDictionary<IdeaKind, double>? KindBoost = null,
    bool PreferSurprise = false)
{
    public static ScoringContext Create(GiftBrief brief, Audience audience, DateOnly today,
        IReadOnlyCollection<string>? previousGiftTitles = null, ScoringWeights? weights = null) =>
        new(brief, audience, today, previousGiftTitles ?? [], weights ?? new ScoringWeights(), brief.Budget);

    /// <summary>Days until the event, or null when the user did not give a date.</summary>
    public int? DaysUntilEvent => Brief.EventDate is { } d ? d.DayNumber - Today.DayNumber : null;
}
