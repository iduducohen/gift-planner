using GiftPlanner.Application.Taxonomy;
using GiftPlanner.Domain.Common;
using GiftPlanner.Domain.Ideas;
using GiftPlanner.Domain.Plans;
using GiftPlanner.Domain.Recipients;

namespace GiftPlanner.Application.Contracts;

internal static class Mapping
{
    public static string KindKey(IdeaKind k) => k switch
    {
        IdeaKind.Experience => "experience",
        IdeaKind.Product => "product",
        IdeaKind.Personal => "personal",
        IdeaKind.Pamper => "pamper",
        IdeaKind.Bundle => "bundle",
        _ => "other"
    };

    public static string KindLabel(IdeaKind k) => k switch
    {
        IdeaKind.Experience => "חוויה",
        IdeaKind.Product => "מתנה",
        IdeaKind.Personal => "מתנה אישית",
        IdeaKind.Pamper => "פינוק",
        IdeaKind.Bundle => "Bundle",
        _ => ""
    };

    public static string SourceKey(PriceSource s) => s switch
    {
        PriceSource.CatalogEstimate => "catalog_estimate",
        PriceSource.ProviderListed => "provider_listed",
        PriceSource.ProviderLive => "provider_live",
        _ => "unknown"
    };

    public static IdeaDto ToDto(this Recommendation r, bool saved) => new(
        r.Id, r.Rank, r.IsTop, r.Title, KindKey(r.Kind), KindLabel(r.Kind), r.Why, r.EmotionalValue, r.HowTo, r.Upgrade,
        r.PriceMin, r.PriceMax, r.ListPrice, r.DiscountPercent, SourceKey(r.PriceSource),
        r.Surprise, r.Effort, r.IconKey, r.IsAiWritten, saved);

    public static bool TryParseEmotion(string value, out Emotion emotion) =>
        Enum.TryParse(value, ignoreCase: true, out emotion) && Enum.IsDefined(emotion);

    public static bool TryParseReaction(string? value, out GiftReaction? reaction)
    {
        reaction = null;
        if (string.IsNullOrWhiteSpace(value)) return true;
        if (Enum.TryParse<GiftReaction>(value, true, out var r) && Enum.IsDefined(r)) { reaction = r; return true; }
        return false;
    }

    public static NextOccasionDto? ToNextDto(this Recipient r, DateOnly today, ITaxonomyService taxonomy) =>
        r.NextOccasion(today) is { } next
            ? new NextOccasionDto(next.Occasion.Id, next.Occasion.TypeKey, taxonomy.OccasionLabel(next.Occasion.TypeKey),
                next.Date, next.Date.DayNumber - today.DayNumber)
            : null;

    public static RecipientSummaryDto ToSummary(this Recipient r, DateOnly today, ITaxonomyService taxonomy) =>
        new(r.Id, r.Nickname, r.RelationshipKey, taxonomy.RelationshipLabel(r.RelationshipKey), r.AgeRangeKey, r.ToNextDto(today, taxonomy));

    public static RecipientDto ToDto(this Recipient r, DateOnly today, ITaxonomyService taxonomy) => new(
        r.Id, r.Nickname, r.RelationshipKey, taxonomy.RelationshipLabel(r.RelationshipKey), r.AgeRangeKey,
        r.Interests, r.Avoid, r.Notes,
        r.Occasions.Select(o => new OccasionDto(o.Id, o.TypeKey, taxonomy.OccasionLabel(o.TypeKey), o.Month, o.Day, o.IsRecurring, o.RemindDaysBefore)).ToList(),
        r.History.OrderByDescending(h => h.GivenOn).Select(h => new GiftHistoryDto(h.Id, h.Title, h.GivenOn, h.Reaction?.ToString())).ToList(),
        r.ToNextDto(today, taxonomy));

    public static PlanDto ToDto(this SurprisePlan p, DateOnly today) => new(
        p.Id, p.RecommendationId, p.RecipientId, p.Title, p.EventDate, p.EventDate.DayNumber - today.DayNumber,
        p.TotalMin, p.TotalMax, p.PlanB,
        p.Steps.Select(s => new PlanStepDto(s.Time.ToString("HH\\:mm"), s.Title, s.Detail, s.CostMin, s.CostMax)).ToList(),
        p.Tasks.Select(t => new PlanTaskDto(t.Id, t.Title, t.DueDate, t.Done)).ToList());
}
