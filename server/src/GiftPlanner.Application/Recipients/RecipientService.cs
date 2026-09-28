using GiftPlanner.Application.Common;
using GiftPlanner.Application.Contracts;
using GiftPlanner.Application.Taxonomy;
using GiftPlanner.Domain.Recipients;

namespace GiftPlanner.Application.Recipients;

public interface IRecipientService
{
    Task<IReadOnlyList<RecipientSummaryDto>> ListAsync(CancellationToken ct);
    Task<Result<RecipientDto>> GetAsync(Guid id, CancellationToken ct);
    Task<Result<RecipientDto>> CreateAsync(UpsertRecipientRequest request, CancellationToken ct);
    Task<Result<RecipientDto>> UpdateAsync(Guid id, UpsertRecipientRequest request, CancellationToken ct);
    Task<Result> DeleteAsync(Guid id, CancellationToken ct);
    Task<Result<RecipientDto>> AddOccasionAsync(Guid id, AddOccasionRequest request, CancellationToken ct);
    Task<Result<RecipientDto>> RemoveOccasionAsync(Guid id, Guid occasionId, CancellationToken ct);
    Task<Result<RecipientDto>> RecordGiftAsync(Guid id, RecordGiftRequest request, CancellationToken ct);
}

public sealed class RecipientService(
    IRecipientRepository recipients,
    ITaxonomyService taxonomy,
    ICurrentUser user,
    IClock clock,
    IUnitOfWork uow) : IRecipientService
{
    public async Task<IReadOnlyList<RecipientSummaryDto>> ListAsync(CancellationToken ct) =>
        (await recipients.ListAsync(user.UserId, ct))
            .Select(r => r.ToSummary(clock.Today, taxonomy))
            .OrderBy(r => r.Next?.DaysLeft ?? int.MaxValue)
            .ThenBy(r => r.Nickname)
            .ToList();

    public async Task<Result<RecipientDto>> GetAsync(Guid id, CancellationToken ct) =>
        await recipients.GetAsync(id, user.UserId, ct) is { } r ? r.ToDto(clock.Today, taxonomy) : Error.NotFound("Recipient");

    public async Task<Result<RecipientDto>> CreateAsync(UpsertRecipientRequest request, CancellationToken ct)
    {
        if (Validate(request) is { } error) return error;
        var r = Recipient.Create(user.UserId, request.Nickname, request.RelationshipKey, request.AgeRangeKey, clock.UtcNow);
        r.SetPreferences(request.Interests, request.Avoid, request.Notes);
        recipients.Add(r);
        await uow.SaveChangesAsync(ct);
        return r.ToDto(clock.Today, taxonomy);
    }

    public Task<Result<RecipientDto>> UpdateAsync(Guid id, UpsertRecipientRequest request, CancellationToken ct) =>
        Validate(request) is { } error
            ? Task.FromResult<Result<RecipientDto>>(error)
            : Mutate(id, r =>
            {
                r.Rename(request.Nickname);
                r.SetBasics(request.RelationshipKey, request.AgeRangeKey);
                r.SetPreferences(request.Interests, request.Avoid, request.Notes);
            }, ct);

    public async Task<Result> DeleteAsync(Guid id, CancellationToken ct)
    {
        var r = await recipients.GetAsync(id, user.UserId, ct);
        if (r is null) return Error.NotFound("Recipient");
        recipients.Remove(r); // hard delete; the person's data leaves with them
        await uow.SaveChangesAsync(ct);
        return Result.Ok();
    }

    public Task<Result<RecipientDto>> AddOccasionAsync(Guid id, AddOccasionRequest request, CancellationToken ct) =>
        !taxonomy.IsKnownOccasion(request.TypeKey)
            ? Task.FromResult<Result<RecipientDto>>(Error.Validation("Unknown occasion."))
            : Mutate(id, r => r.AddOccasion(request.TypeKey, request.Month, request.Day, request.Recurring, request.RemindDaysBefore), ct);

    public Task<Result<RecipientDto>> RemoveOccasionAsync(Guid id, Guid occasionId, CancellationToken ct) =>
        Mutate(id, r => r.RemoveOccasion(occasionId), ct);

    public Task<Result<RecipientDto>> RecordGiftAsync(Guid id, RecordGiftRequest request, CancellationToken ct) =>
        !Mapping.TryParseReaction(request.Reaction, out var reaction)
            ? Task.FromResult<Result<RecipientDto>>(Error.Validation("Unknown reaction."))
            : Mutate(id, r => r.RecordGift(request.Title, request.GivenOn, reaction), ct);

    private async Task<Result<RecipientDto>> Mutate(Guid id, Action<Recipient> change, CancellationToken ct)
    {
        var r = await recipients.GetAsync(id, user.UserId, ct);
        if (r is null) return Error.NotFound("Recipient");
        change(r);
        await uow.SaveChangesAsync(ct);
        return r.ToDto(clock.Today, taxonomy);
    }

    private Error? Validate(UpsertRecipientRequest request) =>
        !taxonomy.IsKnownRelationship(request.RelationshipKey) ? Error.Validation("Unknown relationship.")
        : !taxonomy.IsKnownAgeRange(request.AgeRangeKey) ? Error.Validation("Unknown age range.")
        : null;
}
