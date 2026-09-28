using GiftPlanner.Application.Common;
using GiftPlanner.Application.Contracts;
using GiftPlanner.Domain.Ideas;

namespace GiftPlanner.Application.Saved;

public interface ISavedService
{
    Task<Result> SaveAsync(Guid recommendationId, SaveIdeaRequest request, CancellationToken ct);
    Task<Result> UnsaveAsync(Guid recommendationId, CancellationToken ct);
    Task<IReadOnlyList<SavedIdeaDto>> ListAsync(Guid? recipientId, CancellationToken ct);
}

public sealed class SavedService(
    ISavedIdeaRepository saved,
    IIdeaSetRepository sets,
    IRecipientRepository recipients,
    ICurrentUser user,
    IClock clock,
    IUnitOfWork uow) : ISavedService
{
    public async Task<Result> SaveAsync(Guid recommendationId, SaveIdeaRequest request, CancellationToken ct)
    {
        if (await sets.GetRecommendationAsync(recommendationId, user.UserId, ct) is null) return Error.NotFound("Idea");
        if (request.RecipientId is { } rid && await recipients.GetAsync(rid, user.UserId, ct) is null) return Error.NotFound("Recipient");
        if (await saved.FindAsync(user.UserId, recommendationId, ct) is not null) return Result.Ok(); // idempotent PUT

        saved.Add(SavedIdea.Create(user.UserId, recommendationId, request.RecipientId, clock.UtcNow));
        await uow.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public async Task<Result> UnsaveAsync(Guid recommendationId, CancellationToken ct)
    {
        if (await saved.FindAsync(user.UserId, recommendationId, ct) is { } s)
        {
            saved.Remove(s);
            await uow.SaveChangesAsync(ct);
        }
        return Result.Ok();
    }

    public async Task<IReadOnlyList<SavedIdeaDto>> ListAsync(Guid? recipientId, CancellationToken ct)
    {
        var all = (await saved.ListAsync(user.UserId, ct))
            .Where(s => recipientId is null || s.RecipientId == recipientId)
            .ToList();
        var recs = (await sets.GetRecommendationsAsync(all.Select(s => s.RecommendationId).ToList(), user.UserId, ct))
            .ToDictionary(r => r.Id);
        var names = (await recipients.ListAsync(user.UserId, ct)).ToDictionary(r => r.Id, r => r.Nickname);

        return all
            .Where(s => recs.ContainsKey(s.RecommendationId))
            .OrderByDescending(s => s.SavedAt)
            .Select(s => new SavedIdeaDto(
                recs[s.RecommendationId].ToDto(saved: true),
                s.RecipientId,
                s.RecipientId is { } id && names.TryGetValue(id, out var n) ? n : null,
                s.SavedAt))
            .ToList();
    }
}
