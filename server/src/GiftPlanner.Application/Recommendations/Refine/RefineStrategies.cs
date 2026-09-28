using System.Text.RegularExpressions;
using GiftPlanner.Application.Recommendations.Scoring;
using GiftPlanner.Domain.Common;

namespace GiftPlanner.Application.Recommendations.Refine;

/// <summary>
/// "Not quite it? Change direction." Each preset is a strategy that adjusts the scoring context.
/// Adding a preset = adding a class (Open/Closed); the engine and the API do not change.
/// </summary>
public interface IRefineStrategy
{
    string Key { get; }
    string Label { get; }
    ScoringContext Apply(ScoringContext ctx);
}

public sealed class CheaperStrategy : IRefineStrategy
{
    public string Key => "cheaper";
    public string Label => "יותר זול";
    public ScoringContext Apply(ScoringContext ctx) =>
        ctx with { BudgetCap = Math.Max(50, (int)(ctx.BudgetCap * 0.6)), Weights = ctx.Weights with { Budget = ctx.Weights.Budget * 2 } };
}

public sealed class MoreExcitingStrategy : IRefineStrategy
{
    public string Key => "exciting";
    public string Label => "יותר מרגש";
    public ScoringContext Apply(ScoringContext ctx) =>
        ctx with
        {
            Brief = ctx.Brief.WithEmotions(ctx.Brief.Emotions.Append(Emotion.Moved).Distinct().TakeLast(2)),
            Weights = ctx.Weights with { Emotion = ctx.Weights.Emotion * 1.8 },
            KindBoost = new Dictionary<IdeaKind, double> { [IdeaKind.Experience] = 1.1, [IdeaKind.Personal] = 1.1 }
        };
}

public sealed class MoreSurprisingStrategy : IRefineStrategy
{
    public string Key => "surprise";
    public string Label => "יותר מפתיע";
    public ScoringContext Apply(ScoringContext ctx) =>
        ctx with { PreferSurprise = true, Weights = ctx.Weights with { Novelty = ctx.Weights.Novelty * 2.5 } };
}

public sealed class ExperiencesOnlyStrategy : IRefineStrategy
{
    public string Key => "experiences";
    public string Label => "רק חוויות";
    public ScoringContext Apply(ScoringContext ctx) =>
        ctx with { AllowedKinds = new HashSet<IdeaKind> { IdeaKind.Experience, IdeaKind.Bundle, IdeaKind.Pamper } };
}

public sealed class ProductsOnlyStrategy : IRefineStrategy
{
    public string Key => "products";
    public string Label => "רק מוצרים";
    public ScoringContext Apply(ScoringContext ctx) =>
        ctx with { AllowedKinds = new HashSet<IdeaKind> { IdeaKind.Product, IdeaKind.Personal } };
}

public sealed class MorePersonalStrategy : IRefineStrategy
{
    public string Key => "personal";
    public string Label => "יותר אישי";
    public ScoringContext Apply(ScoringContext ctx) =>
        ctx with { KindBoost = new Dictionary<IdeaKind, double> { [IdeaKind.Personal] = 1.35 } };
}

public interface IRefineStrategyResolver
{
    IReadOnlyList<IRefineStrategy> All { get; }
    IRefineStrategy? Find(string? key);
}

public sealed class RefineStrategyResolver(IEnumerable<IRefineStrategy> strategies) : IRefineStrategyResolver
{
    public IReadOnlyList<IRefineStrategy> All { get; } = strategies.ToList();
    public IRefineStrategy? Find(string? key) => key is null ? null : All.FirstOrDefault(s => s.Key == key);
}

/// <summary>What a free-text refinement ("יש לה כבר הרבה בשמים") means in engine terms.</summary>
public sealed record RefineIntent(string? PresetKey, IReadOnlyList<string> ExtraAvoid, IReadOnlyList<string> ExtraInterests);

public interface IRefineTextInterpreter
{
    Task<RefineIntent> InterpretAsync(string text, CancellationToken ct);
}

/// <summary>
/// Keyword rules for Hebrew free-text refinements. Runs with no LLM and no cost;
/// good enough for the common phrasings and a safe fallback for the LLM interpreter.
/// </summary>
public sealed partial class KeywordRefineInterpreter : IRefineTextInterpreter
{
    private static readonly (string Preset, string[] Words)[] Rules =
    [
        ("cheaper", ["זול", "פחות כסף", "להוציא פחות", "תקציב קטן"]),
        ("surprise", ["מפתיע", "לא מצפה", "בלתי צפוי", "מקורי", "שונה"]),
        ("exciting", ["מרגש", "מרגשת", "רגש", "רומנטי"]),
        ("experiences", ["חוויה", "חוויות", "לצאת", "בילוי"]),
        ("products", ["מוצר", "משהו לשים", "חפץ", "מתנה פיזית"]),
        ("personal", ["אישי", "אישית", "עם משמעות"])
    ];

    [GeneratedRegex(@"(?:כבר\s+(?:יש\s+ל(?:ה|ו)|קיבל(?:ה)?)|לא\s+(?:אוהב(?:ת)?|רוצה))\s+(?:הרבה\s+)?(?<item>[\p{L}\s׳״'-]{2,30})", RegexOptions.CultureInvariant)]
    private static partial Regex AvoidPattern();

    public Task<RefineIntent> InterpretAsync(string text, CancellationToken ct)
    {
        var preset = Rules.FirstOrDefault(r => r.Words.Any(w => text.Contains(w, StringComparison.Ordinal))).Preset;
        var avoid = AvoidPattern().Matches(text)
            .Select(m => m.Groups["item"].Value.Trim().Split(' ', 2)[0])
            .Where(s => s.Length >= 2)
            .SelectMany(w => new[] { w, Singular(w) })
            .Distinct()
            .ToList();
        return Task.FromResult(new RefineIntent(preset, avoid, []));
    }

    /// <summary>Very small Hebrew plural → singular fallback so "בשמים" also matches "בושם" titles.</summary>
    private static string Singular(string word) => word switch
    {
        "בשמים" => "בושם",
        "שרשראות" => "שרשרת",
        "פרחים" => "פרחים",
        _ when word.EndsWith("ים") && word.Length > 4 => word[..^2],
        _ when word.EndsWith("ות") && word.Length > 4 => word[..^2] + "ה",
        _ => word
    };
}
