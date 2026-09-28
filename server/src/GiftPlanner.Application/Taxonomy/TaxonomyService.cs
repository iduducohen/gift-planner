using GiftPlanner.Domain.Common;

namespace GiftPlanner.Application.Taxonomy;

public sealed record OptionDto(string Key, string Label, string? Note = null);

public sealed record OptionGroupDto(string Title, IReadOnlyList<OptionDto> Options);

public sealed record GroupedOptionsDto(IReadOnlyList<OptionDto> Common, IReadOnlyList<OptionGroupDto> Groups);

public sealed record InterestCategoryDto(string Id, string Title, IReadOnlyList<string> Items);

public sealed record InterestsDto(string AudienceLabel, IReadOnlyList<string> Recommended, IReadOnlyList<InterestCategoryDto> Categories, int Total);

public interface ITaxonomyService
{
    GroupedOptionsDto GetRelationships();
    GroupedOptionsDto GetOccasions();
    IReadOnlyList<OptionGroupDto> GetAgeRanges();
    InterestsDto GetInterests(string? relationshipKey, string? ageRangeKey);
    Audience ResolveAudience(string? relationshipKey, string? ageRangeKey);
    Gender ResolveGender(string? relationshipKey);
    string RelationshipLabel(string relationshipKey);
    string OccasionLabel(string occasionKey);
    bool IsKnownRelationship(string key);
    bool IsKnownOccasion(string key);
    bool IsKnownAgeRange(string key);
}

public sealed class TaxonomyService : ITaxonomyService
{
    public GroupedOptionsDto GetRelationships()
    {
        var common = TaxonomyData.CommonRelationships
            .Select(k => TaxonomyData.Relationships.First(r => r.Key == k))
            .Select(r => new OptionDto(r.Key, r.Label))
            .ToList();
        var groups = TaxonomyData.Relationships
            .GroupBy(r => r.Group)
            .Select(g => new OptionGroupDto(g.Key, g.Select(r => new OptionDto(r.Key, r.Label)).ToList()))
            .ToList();
        return new GroupedOptionsDto(common, groups);
    }

    public GroupedOptionsDto GetOccasions()
    {
        var common = TaxonomyData.CommonOccasions
            .Select(k => TaxonomyData.Occasions.First(o => o.Key == k))
            .Select(o => new OptionDto(o.Key, o.Label, o.Note))
            .ToList();
        var groups = TaxonomyData.Occasions
            .GroupBy(o => o.Group)
            .Select(g => new OptionGroupDto(g.Key, g.Select(o => new OptionDto(o.Key, o.Label, o.Note)).ToList()))
            .ToList();
        return new GroupedOptionsDto(common, groups);
    }

    public IReadOnlyList<OptionGroupDto> GetAgeRanges() =>
        TaxonomyData.AgeRanges
            .GroupBy(a => a.Group)
            .Select(g => new OptionGroupDto(g.Key, g.Select(a => new OptionDto(a.Key, a.Label)).ToList()))
            .ToList();

    public InterestsDto GetInterests(string? relationshipKey, string? ageRangeKey)
    {
        var audience = ResolveAudience(relationshipKey, ageRangeKey);
        var categories = TaxonomyData.Interests
            .Where(c => c.Audiences.Contains(audience))
            .Select(c => new InterestCategoryDto(c.Id, c.Title, c.Items))
            .ToList();

        var recommended = relationshipKey is not null
                          && audience == Audience.Adult
                          && TaxonomyData.RecommendedByRelationship.TryGetValue(relationshipKey, out var byRel)
            ? byRel
            : TaxonomyData.RecommendedByAudience[audience];

        var label = relationshipKey is not null && IsKnownRelationship(relationshipKey) ? RelationshipLabel(relationshipKey) : "";
        var age = TaxonomyData.AgeRanges.FirstOrDefault(a => a.Key == ageRangeKey)?.Label;
        var audienceLabel = string.Join(" · ", new[] { label, age }.Where(s => !string.IsNullOrEmpty(s)));

        return new InterestsDto(audienceLabel, recommended, categories, categories.Sum(c => c.Items.Count));
    }

    public Audience ResolveAudience(string? relationshipKey, string? ageRangeKey)
    {
        // Age wins when given; otherwise fall back to what the relationship implies.
        var age = TaxonomyData.AgeRanges.FirstOrDefault(a => a.Key == ageRangeKey);
        if (age is not null) return age.Audience;
        var rel = TaxonomyData.Relationships.FirstOrDefault(r => r.Key == relationshipKey);
        return rel?.FixedAudience ?? Audience.Adult;
    }

    public Gender ResolveGender(string? relationshipKey) =>
        TaxonomyData.Relationships.FirstOrDefault(r => r.Key == relationshipKey)?.Gender ?? Gender.Neutral;

    public string RelationshipLabel(string relationshipKey) =>
        TaxonomyData.Relationships.FirstOrDefault(r => r.Key == relationshipKey)?.Label ?? relationshipKey;

    public string OccasionLabel(string occasionKey) =>
        TaxonomyData.Occasions.FirstOrDefault(o => o.Key == occasionKey)?.Label ?? occasionKey;

    public bool IsKnownRelationship(string key) => TaxonomyData.Relationships.Any(r => r.Key == key);
    public bool IsKnownOccasion(string key) => TaxonomyData.Occasions.Any(o => o.Key == key);
    public bool IsKnownAgeRange(string key) => TaxonomyData.AgeRanges.Any(a => a.Key == key);
}
