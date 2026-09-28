using GiftPlanner.Application.Common;
using GiftPlanner.Domain.Catalog;
using GiftPlanner.Domain.GiftSessions;
using GiftPlanner.Domain.Ideas;
using GiftPlanner.Domain.Orders;
using GiftPlanner.Domain.Plans;
using GiftPlanner.Domain.Recipients;
using GiftPlanner.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace GiftPlanner.Infrastructure.Persistence;

// Every query is scoped by UserId in the repository itself, so a controller can never forget
// an ownership check (prevents IDOR by construction).

internal sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> GetAsync(Guid id, CancellationToken ct) => db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<User?> FindByExternalAsync(string provider, string externalId, CancellationToken ct) =>
        db.Users.FirstOrDefaultAsync(u => u.ExternalProvider == provider && u.ExternalId == externalId, ct);

    public void Add(User user) => db.Users.Add(user);
}

internal sealed class RecipientRepository(AppDbContext db) : IRecipientRepository
{
    public Task<Recipient?> GetAsync(Guid id, Guid userId, CancellationToken ct) =>
        db.Recipients.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId, ct);

    public async Task<IReadOnlyList<Recipient>> ListAsync(Guid userId, CancellationToken ct) =>
        await db.Recipients.Where(r => r.UserId == userId).AsSplitQuery().ToListAsync(ct);

    public void Add(Recipient recipient) => db.Recipients.Add(recipient);
    public void Remove(Recipient recipient) => db.Recipients.Remove(recipient);
}

internal sealed class GiftSessionRepository(AppDbContext db) : IGiftSessionRepository
{
    public Task<GiftSession?> GetAsync(Guid id, Guid userId, CancellationToken ct) =>
        db.GiftSessions.FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId, ct);

    public void Add(GiftSession session) => db.GiftSessions.Add(session);
}

internal sealed class IdeaSetRepository(AppDbContext db) : IIdeaSetRepository
{
    public Task<IdeaSet?> GetAsync(Guid id, Guid userId, CancellationToken ct) =>
        db.IdeaSets.Include(s => s.Items).FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId, ct);

    public Task<Recommendation?> GetRecommendationAsync(Guid recommendationId, Guid userId, CancellationToken ct) =>
        (from r in db.Recommendations
         join s in db.IdeaSets on r.IdeaSetId equals s.Id
         where r.Id == recommendationId && s.UserId == userId
         select r).FirstOrDefaultAsync(ct);

    public async Task<IReadOnlyList<Recommendation>> GetRecommendationsAsync(IReadOnlyCollection<Guid> ids, Guid userId, CancellationToken ct) =>
        await (from r in db.Recommendations
               join s in db.IdeaSets on r.IdeaSetId equals s.Id
               where ids.Contains(r.Id) && s.UserId == userId
               select r).ToListAsync(ct);

    public void Add(IdeaSet set) => db.IdeaSets.Add(set);
}

internal sealed class SavedIdeaRepository(AppDbContext db) : ISavedIdeaRepository
{
    public Task<SavedIdea?> FindAsync(Guid userId, Guid recommendationId, CancellationToken ct) =>
        db.SavedIdeas.FirstOrDefaultAsync(s => s.UserId == userId && s.RecommendationId == recommendationId, ct);

    public async Task<IReadOnlyList<SavedIdea>> ListAsync(Guid userId, CancellationToken ct) =>
        await db.SavedIdeas.Where(s => s.UserId == userId).ToListAsync(ct);

    public void Add(SavedIdea saved) => db.SavedIdeas.Add(saved);
    public void Remove(SavedIdea saved) => db.SavedIdeas.Remove(saved);
}

internal sealed class PlanRepository(AppDbContext db) : IPlanRepository
{
    public Task<SurprisePlan?> GetAsync(Guid id, Guid userId, CancellationToken ct) =>
        db.Plans.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId, ct);

    public Task<SurprisePlan?> FindByRecommendationAsync(Guid recommendationId, Guid userId, CancellationToken ct) =>
        db.Plans.FirstOrDefaultAsync(p => p.RecommendationId == recommendationId && p.UserId == userId, ct);

    public async Task<IReadOnlyList<SurprisePlan>> ListAsync(Guid userId, CancellationToken ct) =>
        await db.Plans.Where(p => p.UserId == userId).AsSplitQuery().ToListAsync(ct);

    public void Add(SurprisePlan plan) => db.Plans.Add(plan);
}

internal sealed class PurchaseIntentRepository(AppDbContext db) : IPurchaseIntentRepository
{
    public void Add(PurchaseIntent intent) => db.PurchaseIntents.Add(intent);
}

/// <summary>The catalog is small (hundreds of rows) and read on every request: cache it in process for a few minutes.</summary>
internal sealed class CatalogReader(AppDbContext db, IMemoryCache cache) : ICatalogReader
{
    private const string Key = "catalog:active";

    public async Task<IReadOnlyList<CatalogIdea>> GetActiveIdeasAsync(CancellationToken ct) =>
        await cache.GetOrCreateAsync(Key, async e =>
        {
            e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
            return (IReadOnlyList<CatalogIdea>)await db.CatalogIdeas.AsNoTracking().Where(i => i.IsActive).ToListAsync(ct);
        }) ?? [];
}

/// <summary>Offers negotiated with partners and stored in our own catalog (the MVP provider).</summary>
internal sealed class CuratedCatalogOfferProvider(AppDbContext db) : IOfferProvider
{
    public string Key => "curated";

    public async Task<IReadOnlyList<OfferQuote>> QuoteAsync(IReadOnlyCollection<Guid> ideaIds, CancellationToken ct)
    {
        if (ideaIds.Count == 0) return [];
        var rows = await (from o in db.Offers.AsNoTracking()
                          join p in db.Partners.AsNoTracking() on o.PartnerId equals p.Id
                          where ideaIds.Contains(o.CatalogIdeaId)
                          select new { o, p }).ToListAsync(ct);
        return rows.Select(x => new OfferQuote(
            x.o.Id, x.o.CatalogIdeaId, x.p.Id, x.p.Name, x.p.Region, x.p.IsVerified,
            x.o.ListPrice, x.o.DiscountPercent, x.o.FinalPrice, x.o.Url, x.o.CouponCode,
            x.o.PriceSource, x.o.VerifiedAt, x.o.IsSponsored, x.p.Phone, x.p.ImageUrl)).ToList();
    }
}

internal sealed class UserDataMaintenance(AppDbContext db) : IUserDataMaintenance
{
    public async Task MergeAsync(Guid fromUserId, Guid toUserId, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // Saved ideas: skip the ones the target user already saved (unique index).
        var alreadySaved = await db.SavedIdeas.Where(s => s.UserId == toUserId).Select(s => s.RecommendationId).ToListAsync(ct);
        await db.SavedIdeas.Where(s => s.UserId == fromUserId && alreadySaved.Contains(s.RecommendationId)).ExecuteDeleteAsync(ct);

        await db.Recipients.Where(x => x.UserId == fromUserId).ExecuteUpdateAsync(s => s.SetProperty(x => x.UserId, toUserId), ct);
        await db.GiftSessions.Where(x => x.UserId == fromUserId).ExecuteUpdateAsync(s => s.SetProperty(x => x.UserId, toUserId), ct);
        await db.IdeaSets.Where(x => x.UserId == fromUserId).ExecuteUpdateAsync(s => s.SetProperty(x => x.UserId, toUserId), ct);
        await db.SavedIdeas.Where(x => x.UserId == fromUserId).ExecuteUpdateAsync(s => s.SetProperty(x => x.UserId, toUserId), ct);
        await db.Plans.Where(x => x.UserId == fromUserId).ExecuteUpdateAsync(s => s.SetProperty(x => x.UserId, toUserId), ct);
        await db.PurchaseIntents.Where(x => x.UserId == fromUserId).ExecuteUpdateAsync(s => s.SetProperty(x => x.UserId, toUserId), ct);
        await db.Users.Where(u => u.Id == fromUserId).ExecuteDeleteAsync(ct);

        await tx.CommitAsync(ct);
    }

    public async Task<object> ExportAsync(Guid userId, CancellationToken ct) => new
    {
        user = await db.Users.AsNoTracking().Where(u => u.Id == userId)
            .Select(u => new { u.Id, u.ExternalProvider, u.DisplayName, u.CreatedAt }).FirstOrDefaultAsync(ct),
        recipients = await db.Recipients.AsNoTracking().Where(r => r.UserId == userId).ToListAsync(ct),
        sessions = await db.GiftSessions.AsNoTracking().Where(s => s.UserId == userId)
            .Select(s => new { s.Id, s.Brief, s.CreatedAt }).ToListAsync(ct),
        saved = await db.SavedIdeas.AsNoTracking().Where(s => s.UserId == userId).ToListAsync(ct),
        plans = await db.Plans.AsNoTracking().Where(p => p.UserId == userId).ToListAsync(ct),
        purchases = await db.PurchaseIntents.AsNoTracking().Where(p => p.UserId == userId).ToListAsync(ct)
    };

    /// <summary>Hard delete of everything the user owns. Recommendations cascade from their idea sets.</summary>
    public async Task DeleteAllAsync(Guid userId, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        await db.SavedIdeas.Where(x => x.UserId == userId).ExecuteDeleteAsync(ct);
        await db.PurchaseIntents.Where(x => x.UserId == userId).ExecuteDeleteAsync(ct);
        await db.Plans.Where(x => x.UserId == userId).ExecuteDeleteAsync(ct);
        await db.IdeaSets.Where(x => x.UserId == userId).ExecuteDeleteAsync(ct);
        await db.GiftSessions.Where(x => x.UserId == userId).ExecuteDeleteAsync(ct);
        await db.Recipients.Where(x => x.UserId == userId).ExecuteDeleteAsync(ct);
        await db.Users.Where(x => x.Id == userId).ExecuteDeleteAsync(ct);
        await tx.CommitAsync(ct);
    }
}
