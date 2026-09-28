using GiftPlanner.Domain.Common;
using GiftPlanner.Domain.GiftSessions;

namespace GiftPlanner.Application.Recommendations.Clarification;

/// <summary>Decides whether one extra question would change the ideas enough to be worth asking.</summary>
public interface IClarificationPolicy
{
    ClarifyingQuestion? Decide(GiftBrief brief);

    /// <summary>Applies the chosen answer to the brief.</summary>
    GiftBrief Apply(GiftBrief brief, ClarifyingQuestion question, string answerKey);
}

/// <summary>
/// Rule: ask only when we know fewer than two interests AND no emotion was chosen, or when the
/// chosen emotions pull in opposite directions. Otherwise never ask; speed beats precision here.
/// </summary>
public sealed class RuleBasedClarificationPolicy : IClarificationPolicy
{
    public const string DirectionKey = "direction";

    private static readonly ClarifyingQuestion Direction = new(
        DirectionKey,
        "מה ישמח יותר?",
        [
            new("romantic", "חוויה בשניים", "ערב, יציאה, זמן יחד"),
            new("pamper", "פינוק רק בשבילו/ה", "זמן לעצמו/ה, בלי להתאמץ"),
            new("unexpected", "משהו שלא מצפים לו בכלל", "הפתעה אמיתית, מחוץ לקופסה")
        ]);

    public ClarifyingQuestion? Decide(GiftBrief brief)
    {
        var tooLittleInfo = brief.Interests.Count < 2 && brief.Emotions.Count == 0;
        var conflicting = brief.Emotions.Contains(Emotion.Pampered) && brief.Emotions.Contains(Emotion.Adventure);
        return tooLittleInfo || conflicting ? Direction : null;
    }

    public GiftBrief Apply(GiftBrief brief, ClarifyingQuestion question, string answerKey) =>
        answerKey switch
        {
            "romantic" => brief.WithEmotions([Emotion.Moved]),
            "pamper" => brief.WithEmotions([Emotion.Pampered]),
            "unexpected" => brief.WithEmotions([Emotion.Surprised]),
            _ => brief
        };
}
