using GiftPlanner.Domain.Catalog;

namespace GiftPlanner.Application.Recommendations.Scoring;

/// <summary>A hard rule: an idea that fails any filter is never shown, however well it scores.</summary>
public interface IHardFilter
{
    string Name { get; }
    bool Allows(CatalogIdea idea, ScoringContext ctx);
}

/// <summary>Kids get kids' ideas; adults do not get toy suggestions.</summary>
public sealed class AudienceFilter : IHardFilter
{
    public string Name => "audience";
    public bool Allows(CatalogIdea idea, ScoringContext ctx) => idea.Audiences.Count == 0 || idea.Audiences.Contains(ctx.Audience);
}

/// <summary>Drops ideas whose cheapest price is more than 15% over the budget (or the refine cap).</summary>
public sealed class BudgetFilter : IHardFilter
{
    public const double Tolerance = 1.15;
    public string Name => "budget";
    public bool Allows(CatalogIdea idea, ScoringContext ctx) => ctx.BudgetCap <= 0 || idea.PriceMin <= ctx.BudgetCap * Tolerance;
}

/// <summary>"Already has / doesn't like": matched against the title and the idea's interests.</summary>
public sealed class AvoidFilter : IHardFilter
{
    public string Name => "avoid";

    public bool Allows(CatalogIdea idea, ScoringContext ctx) =>
        !ctx.Brief.Avoid.Any(term =>
            idea.Title.Contains(term, StringComparison.OrdinalIgnoreCase)
            || idea.Interests.Any(i => i.Equals(term, StringComparison.OrdinalIgnoreCase))
            || idea.Includes.Any(i => i.Contains(term, StringComparison.OrdinalIgnoreCase)));
}

/// <summary>Never suggest something the recipient already received.</summary>
public sealed class HistoryFilter : IHardFilter
{
    public string Name => "history";
    public bool Allows(CatalogIdea idea, ScoringContext ctx) =>
        !ctx.PreviousGiftTitles.Any(t => t.Equals(idea.Title, StringComparison.OrdinalIgnoreCase));
}

/// <summary>If the event is in 2 days, a gift that needs a week of lead time is useless.</summary>
public sealed class LeadTimeFilter : IHardFilter
{
    public string Name => "lead_time";
    public bool Allows(CatalogIdea idea, ScoringContext ctx) => ctx.DaysUntilEvent is not { } days || days < 0 || idea.LeadTimeDays <= days;
}

/// <summary>Applied by "experiences only" / "products only" refinements.</summary>
public sealed class KindFilter : IHardFilter
{
    public string Name => "kind";
    public bool Allows(CatalogIdea idea, ScoringContext ctx) => ctx.AllowedKinds is null || ctx.AllowedKinds.Contains(idea.Kind);
}
