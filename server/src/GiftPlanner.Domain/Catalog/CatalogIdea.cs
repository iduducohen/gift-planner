using GiftPlanner.Domain.Common;

namespace GiftPlanner.Domain.Catalog;

/// <summary>
/// A curated, human-tagged gift idea. This is the "ground truth" the recommendation engine ranks.
/// The LLM may only pick from and write about catalog ideas; it never invents prices or suppliers.
/// </summary>
public sealed class CatalogIdea : Entity
{
    public string Title { get; private set; } = "";
    public string Description { get; private set; } = "";
    public IdeaKind Kind { get; private set; }
    public List<string> Interests { get; private set; } = [];
    public List<Emotion> Emotions { get; private set; } = [];
    /// <summary>Relationship keys this idea suits. Empty means "any".</summary>
    public List<string> Relationships { get; private set; } = [];
    public List<Audience> Audiences { get; private set; } = [];
    /// <summary>Occasion keys this idea suits. Empty means "any".</summary>
    public List<string> Occasions { get; private set; } = [];
    public int PriceMin { get; private set; }
    public int PriceMax { get; private set; }
    public int Surprise { get; private set; }
    public int Effort { get; private set; }
    public int Novelty { get; private set; }
    public int LeadTimeDays { get; private set; }
    public string IconKey { get; private set; } = "gift";
    public string? ImageUrl { get; private set; }
    public string? Upgrade { get; private set; }
    public List<string> Includes { get; private set; } = [];
    public DateOnly VerifiedAt { get; private set; }
    public bool IsActive { get; private set; } = true;

    private CatalogIdea() { }

    public static CatalogIdea Create(
        Guid id, string title, string description, IdeaKind kind,
        IEnumerable<string> interests, IEnumerable<Emotion> emotions, IEnumerable<string> relationships,
        IEnumerable<Audience> audiences, IEnumerable<string> occasions,
        int priceMin, int priceMax, int surprise, int effort, int novelty, int leadTimeDays,
        string iconKey, string? upgrade, IEnumerable<string> includes, DateOnly verifiedAt, string? imageUrl = null)
    {
        if (priceMin < 0 || priceMax < priceMin)
            throw new DomainException("validation", "Invalid price range.");
        static int Level(int v) => v is < 1 or > 5 ? throw new DomainException("validation", "Levels are 1–5.") : v;

        return new CatalogIdea
        {
            Id = id,
            Title = Guard.NotBlank(title, nameof(title), 120),
            Description = description.Trim(),
            Kind = kind,
            Interests = interests.ToList(),
            Emotions = emotions.ToList(),
            Relationships = relationships.ToList(),
            Audiences = audiences.ToList(),
            Occasions = occasions.ToList(),
            PriceMin = priceMin,
            PriceMax = priceMax,
            Surprise = Level(surprise),
            Effort = Level(effort),
            Novelty = Level(novelty),
            LeadTimeDays = Math.Max(0, leadTimeDays),
            IconKey = iconKey,
            ImageUrl = imageUrl,
            Upgrade = upgrade,
            Includes = includes.ToList(),
            VerifiedAt = verifiedAt
        };
    }

    public void Deactivate() => IsActive = false;
}

/// <summary>A business that offers catalog ideas with an exclusive discount for app users.</summary>
public sealed class Partner : Entity
{
    public string Name { get; private set; } = "";
    public string Region { get; private set; } = "";
    public string Website { get; private set; } = "";
    public string? ImageUrl { get; private set; }
    public string? Phone { get; private set; }
    public bool IsVerified { get; private set; }

    private Partner() { }

    public static Partner Create(Guid id, string name, string region, string website, bool verified, string? imageUrl = null, string? phone = null) =>
        new() { Id = id, Name = Guard.NotBlank(name, nameof(name)), Region = region, Website = website, IsVerified = verified, ImageUrl = imageUrl, Phone = phone };
}

/// <summary>A purchasable offer for a catalog idea from a partner, including the negotiated discount.</summary>
public sealed class Offer : Entity
{
    public Guid CatalogIdeaId { get; private set; }
    public Guid PartnerId { get; private set; }
    public int ListPrice { get; private set; }
    public int DiscountPercent { get; private set; }
    public string Url { get; private set; } = "";
    public string CouponCode { get; private set; } = "";
    public PriceSource PriceSource { get; private set; }
    public DateOnly VerifiedAt { get; private set; }
    /// <summary>Sponsored placement flag. Never used by the scoring engine.</summary>
    public bool IsSponsored { get; private set; }

    public int FinalPrice => (int)Math.Round(ListPrice * (100 - DiscountPercent) / 100m, MidpointRounding.AwayFromZero);

    private Offer() { }

    public static Offer Create(Guid id, Guid ideaId, Guid partnerId, int listPrice, int discountPercent, string url,
        string couponCode, PriceSource source, DateOnly verifiedAt, bool sponsored = false)
    {
        if (discountPercent is < 0 or > 90) throw new DomainException("validation", "Discount must be 0–90%.");
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            throw new DomainException("validation", "Offer URL must be an absolute https URL.");
        return new Offer
        {
            Id = id, CatalogIdeaId = ideaId, PartnerId = partnerId, ListPrice = listPrice,
            DiscountPercent = discountPercent, Url = url, CouponCode = couponCode,
            PriceSource = source, VerifiedAt = verifiedAt, IsSponsored = sponsored
        };
    }
}
