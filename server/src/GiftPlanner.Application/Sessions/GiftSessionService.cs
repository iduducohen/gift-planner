using GiftPlanner.Application.Common;
using GiftPlanner.Application.Contracts;
using GiftPlanner.Application.Recommendations.Clarification;
using GiftPlanner.Application.Taxonomy;
using GiftPlanner.Domain.Common;
using GiftPlanner.Domain.GiftSessions;
using GiftPlanner.Domain.Recipients;

namespace GiftPlanner.Application.Sessions;

public interface IGiftSessionService
{
    Task<Result<SessionDto>> CreateAsync(CreateSessionRequest request, CancellationToken ct);
    Task<Result<SessionDto>> ClarifyAsync(Guid sessionId, ClarifyRequest request, CancellationToken ct);
    Task<Result<RecipientDto>> SaveRecipientAsync(Guid sessionId, string nickname, CancellationToken ct);
}

public sealed class GiftSessionService(
    IGiftSessionRepository sessions,
    IRecipientRepository recipients,
    IClarificationPolicy clarification,
    ITaxonomyService taxonomy,
    ICurrentUser user,
    IClock clock,
    IUnitOfWork uow) : IGiftSessionService
{
    public async Task<Result<SessionDto>> CreateAsync(CreateSessionRequest request, CancellationToken ct)
    {
        if (!taxonomy.IsKnownRelationship(request.RelationshipKey)) return Error.Validation("Unknown relationship.");
        if (!taxonomy.IsKnownOccasion(request.OccasionKey)) return Error.Validation("Unknown occasion.");
        if (!taxonomy.IsKnownAgeRange(request.AgeRangeKey)) return Error.Validation("Unknown age range.");

        var emotions = new List<Emotion>();
        foreach (var e in request.Emotions ?? [])
        {
            if (!Mapping.TryParseEmotion(e, out var parsed)) return Error.Validation($"Unknown emotion '{e}'.");
            emotions.Add(parsed);
        }

        Recipient? recipient = null;
        if (request.RecipientId is { } rid)
        {
            recipient = await recipients.GetAsync(rid, user.UserId, ct);
            if (recipient is null) return Error.NotFound("Recipient");
        }

        var brief = GiftBrief.Create(request.RelationshipKey, request.OccasionKey, request.EventDate, request.AgeRangeKey,
            request.Interests, request.Avoid, request.Notes, request.Budget, emotions);

        // Whatever the user tells us about a saved person improves their profile for next time.
        recipient?.SetPreferences(recipient.Interests.Union(brief.Interests), recipient.Avoid.Union(brief.Avoid), brief.Notes ?? recipient.Notes);

        var session = GiftSession.Start(user.UserId, recipient?.Id, brief, clock.UtcNow);
        if (clarification.Decide(brief) is { } question) session.Ask(question);

        sessions.Add(session);
        await uow.SaveChangesAsync(ct);
        return ToDto(session);
    }

    public async Task<Result<SessionDto>> ClarifyAsync(Guid sessionId, ClarifyRequest request, CancellationToken ct)
    {
        var session = await sessions.GetAsync(sessionId, user.UserId, ct);
        if (session is null) return Error.NotFound("Session");
        if (session.Question is null || session.Status != GiftSessionStatus.AwaitingClarification)
            return Error.Conflict("There is no open question.");

        var refined = request.AnswerKey is null ? null : clarification.Apply(session.Brief, session.Question, request.AnswerKey);
        session.Resolve(request.AnswerKey, refined);
        await uow.SaveChangesAsync(ct);
        return ToDto(session);
    }

    /// <summary>"Save this person": creates a recipient from the session's brief, including the event date as an occasion.</summary>
    public async Task<Result<RecipientDto>> SaveRecipientAsync(Guid sessionId, string nickname, CancellationToken ct)
    {
        var session = await sessions.GetAsync(sessionId, user.UserId, ct);
        if (session is null) return Error.NotFound("Session");
        if (session.RecipientId is { } existingId && await recipients.GetAsync(existingId, user.UserId, ct) is { } existing)
            return existing.ToDto(clock.Today, taxonomy);

        var b = session.Brief;
        var recipient = Recipient.Create(user.UserId, nickname, b.RelationshipKey, b.AgeRangeKey, clock.UtcNow);
        recipient.SetPreferences(b.Interests, b.Avoid, b.Notes);
        if (b.EventDate is { } date)
            recipient.AddOccasion(b.OccasionKey, date.Month, date.Day, recurring: true, remindDaysBefore: 10);

        recipients.Add(recipient);
        session.AttachRecipient(recipient.Id);
        await uow.SaveChangesAsync(ct);
        return recipient.ToDto(clock.Today, taxonomy);
    }

    private static SessionDto ToDto(GiftSession s) => new(
        s.Id,
        s.Status == GiftSessionStatus.AwaitingClarification ? "awaiting_clarification" : "ready",
        s.Status == GiftSessionStatus.AwaitingClarification && s.Question is { } q
            ? new QuestionDto(q.Key, q.Text, q.Options.Select(o => new QuestionOptionDto(o.Key, o.Label, o.Hint)).ToList())
            : null);
}
