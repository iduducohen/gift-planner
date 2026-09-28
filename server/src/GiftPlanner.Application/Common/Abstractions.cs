using GiftPlanner.Domain.Catalog;
using GiftPlanner.Domain.Common;
using GiftPlanner.Domain.GiftSessions;
using GiftPlanner.Domain.Ideas;
using GiftPlanner.Domain.Orders;
using GiftPlanner.Domain.Plans;
using GiftPlanner.Domain.Recipients;
using GiftPlanner.Domain.Users;

namespace GiftPlanner.Application.Common;

// Ports. The Application layer owns these interfaces (Dependency Inversion); Infrastructure implements them.
// Each interface is small and role-specific (Interface Segregation).

public interface IClock
{
    DateTimeOffset UtcNow { get; }
    DateOnly Today { get; }
}

public interface ICurrentUser
{
    Guid UserId { get; }
    bool IsAnonymous { get; }
}

public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken ct);
}

public interface IUserRepository
{
    Task<User?> GetAsync(Guid id, CancellationToken ct);
    Task<User?> FindByExternalAsync(string provider, string externalId, CancellationToken ct);
    void Add(User user);
}

public interface IRecipientRepository
{
    Task<Recipient?> GetAsync(Guid id, Guid userId, CancellationToken ct);
    Task<IReadOnlyList<Recipient>> ListAsync(Guid userId, CancellationToken ct);
    void Add(Recipient recipient);
    void Remove(Recipient recipient);
}

public interface IGiftSessionRepository
{
    Task<GiftSession?> GetAsync(Guid id, Guid userId, CancellationToken ct);
    void Add(GiftSession session);
}

public interface IIdeaSetRepository
{
    Task<IdeaSet?> GetAsync(Guid id, Guid userId, CancellationToken ct);
    Task<Recommendation?> GetRecommendationAsync(Guid recommendationId, Guid userId, CancellationToken ct);
    Task<IReadOnlyList<Recommendation>> GetRecommendationsAsync(IReadOnlyCollection<Guid> ids, Guid userId, CancellationToken ct);
    void Add(IdeaSet set);
}

public interface ISavedIdeaRepository
{
    Task<SavedIdea?> FindAsync(Guid userId, Guid recommendationId, CancellationToken ct);
    Task<IReadOnlyList<SavedIdea>> ListAsync(Guid userId, CancellationToken ct);
    void Add(SavedIdea saved);
    void Remove(SavedIdea saved);
}

public interface IPlanRepository
{
    Task<SurprisePlan?> GetAsync(Guid id, Guid userId, CancellationToken ct);
    Task<SurprisePlan?> FindByRecommendationAsync(Guid recommendationId, Guid userId, CancellationToken ct);
    Task<IReadOnlyList<SurprisePlan>> ListAsync(Guid userId, CancellationToken ct);
    void Add(SurprisePlan plan);
}

public interface IPurchaseIntentRepository
{
    void Add(PurchaseIntent intent);
}

/// <summary>Read-only access to the curated catalog (the engine never writes to it).</summary>
public interface ICatalogReader
{
    Task<IReadOnlyList<CatalogIdea>> GetActiveIdeasAsync(CancellationToken ct);
}

/// <summary>A price quote from a partner, carrying where the price came from.</summary>
public sealed record OfferQuote(
    Guid OfferId, Guid IdeaId, Guid PartnerId, string PartnerName, string PartnerRegion, bool PartnerVerified,
    int ListPrice, int DiscountPercent, int FinalPrice, string Url, string CouponCode,
    PriceSource PriceSource, DateOnly VerifiedAt, bool IsSponsored, string? PartnerPhone = null, string? PartnerImage = null);

/// <summary>
/// Open/Closed extension point for suppliers. Adding flowers, vouchers or hotels = a new implementation
/// registered in DI; the recommendation engine does not change.
/// </summary>
public interface IOfferProvider
{
    string Key { get; }
    Task<IReadOnlyList<OfferQuote>> QuoteAsync(IReadOnlyCollection<Guid> ideaIds, CancellationToken ct);
}

public interface ICacheService
{
    Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan ttl, CancellationToken ct);
    Task RemoveAsync(string key, CancellationToken ct);
}

public enum LlmTier
{
    /// <summary>Small, fast, cheap model: parsing and classification.</summary>
    Fast = 1,
    /// <summary>Stronger model: writing the copy for the top ideas.</summary>
    Smart = 2
}

public sealed record LlmRequest(LlmTier Tier, string System, string User, int MaxTokens, double Temperature = 0.4);

public sealed record LlmResult(string Text, int InputTokens, int OutputTokens, string Model);

public interface ILlmClient
{
    bool IsConfigured { get; }
    Task<LlmResult> CompleteAsync(LlmRequest request, CancellationToken ct);
}

public sealed record ExternalIdentity(string Provider, string ExternalId, string? DisplayName);

public interface IExternalIdentityVerifier
{
    string Provider { get; }
    Task<ExternalIdentity?> VerifyAsync(string idToken, CancellationToken ct);
}

public interface ITokenIssuer
{
    string Issue(User user);
}

/// <summary>Cross-aggregate maintenance on a user's data (merge on sign-in, export, erase).</summary>
public interface IUserDataMaintenance
{
    Task MergeAsync(Guid fromUserId, Guid toUserId, CancellationToken ct);
    Task<object> ExportAsync(Guid userId, CancellationToken ct);
    Task DeleteAllAsync(Guid userId, CancellationToken ct);
}
