using GiftPlanner.Domain.Common;

namespace GiftPlanner.Domain.Ideas;

/// <summary>
/// One ranked list of ideas for a session. A refinement creates a new set pointing at its parent,
/// so the user can always go back and we keep an audit trail of what the AI produced.
/// </summary>
public sealed class IdeaSet : Entity
{
    private readonly List<Recommendation> _items = [];

    public Guid SessionId { get; private set; }
    public Guid UserId { get; private set; }
    public Guid? ParentSetId { get; private set; }
    public string? RefinePreset { get; private set; }
    public string? RefineText { get; private set; }
    public string ComposerName { get; private set; } = "";
    public int TokensIn { get; private set; }
    public int TokensOut { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyList<Recommendation> Items => _items;

    private IdeaSet() { }

    public static IdeaSet Create(Guid sessionId, Guid userId, Guid? parentSetId, string? refinePreset, string? refineText,
        IEnumerable<Recommendation> items, string composerName, int tokensIn, int tokensOut, DateTimeOffset now)
    {
        var set = new IdeaSet
        {
            SessionId = sessionId, UserId = userId, ParentSetId = parentSetId,
            RefinePreset = refinePreset, RefineText = refineText,
            ComposerName = composerName, TokensIn = tokensIn, TokensOut = tokensOut, CreatedAt = now
        };
        var rank = 1;
        foreach (var item in items)
        {
            item.AttachTo(set.Id, rank++);
            set._items.Add(item);
        }
        if (set._items.Count == 0) throw new DomainException("no_ideas", "No ideas matched this brief.");
        return set;
    }
}

/// <summary>A single idea shown to the user. Prices are copied from the catalog/offer, never from the LLM.</summary>
public sealed class Recommendation : Entity
{
    public Guid IdeaSetId { get; private set; }
    public int Rank { get; private set; }
    public bool IsTop { get; private set; }
    public Guid CatalogIdeaId { get; private set; }
    public string Title { get; private set; } = "";
    public IdeaKind Kind { get; private set; }
    public string Why { get; private set; } = "";
    public string? EmotionalValue { get; private set; }
    public string? HowTo { get; private set; }
    public string? Upgrade { get; private set; }
    public int PriceMin { get; private set; }
    public int PriceMax { get; private set; }
    public int? ListPrice { get; private set; }
    public int? DiscountPercent { get; private set; }
    public Guid? OfferId { get; private set; }
    public PriceSource PriceSource { get; private set; }
    public double Score { get; private set; }
    public Dictionary<string, double> ScoreBreakdown { get; private set; } = [];
    public int Surprise { get; private set; }
    public int Effort { get; private set; }
    public string IconKey { get; private set; } = "gift";
    public bool IsAiWritten { get; private set; }

    private Recommendation() { }

    public static Recommendation Create(Guid catalogIdeaId, bool isTop, string title, IdeaKind kind, string why,
        string? emotionalValue, string? howTo, string? upgrade, int priceMin, int priceMax, int? listPrice,
        int? discountPercent, Guid? offerId, PriceSource source, double score, Dictionary<string, double> breakdown,
        int surprise, int effort, string iconKey, bool aiWritten) =>
        new()
        {
            CatalogIdeaId = catalogIdeaId, IsTop = isTop, Title = title, Kind = kind, Why = why,
            EmotionalValue = emotionalValue, HowTo = howTo, Upgrade = upgrade,
            PriceMin = priceMin, PriceMax = priceMax, ListPrice = listPrice, DiscountPercent = discountPercent,
            OfferId = offerId, PriceSource = source, Score = Math.Round(score, 4), ScoreBreakdown = breakdown,
            Surprise = surprise, Effort = effort, IconKey = iconKey, IsAiWritten = aiWritten
        };

    internal void AttachTo(Guid setId, int rank)
    {
        IdeaSetId = setId;
        Rank = rank;
    }
}

/// <summary>A recommendation the user hearted, optionally tied to a recipient.</summary>
public sealed class SavedIdea : Entity
{
    public Guid UserId { get; private set; }
    public Guid RecommendationId { get; private set; }
    public Guid? RecipientId { get; private set; }
    public DateTimeOffset SavedAt { get; private set; }

    private SavedIdea() { }

    public static SavedIdea Create(Guid userId, Guid recommendationId, Guid? recipientId, DateTimeOffset now) =>
        new() { UserId = userId, RecommendationId = recommendationId, RecipientId = recipientId, SavedAt = now };
}
