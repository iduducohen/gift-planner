using GiftPlanner.Domain.Common;

namespace GiftPlanner.Domain.GiftSessions;

/// <summary>Everything the user told us about the recipient and the occasion, in one immutable value.</summary>
public sealed record GiftBrief(
    string RelationshipKey,
    string OccasionKey,
    DateOnly? EventDate,
    string AgeRangeKey,
    IReadOnlyList<string> Interests,
    IReadOnlyList<string> Avoid,
    string? Notes,
    int Budget,
    IReadOnlyList<Emotion> Emotions)
{
    public const int MaxBudget = 20_000;

    public static GiftBrief Create(string relationshipKey, string occasionKey, DateOnly? eventDate, string ageRangeKey,
        IEnumerable<string>? interests, IEnumerable<string>? avoid, string? notes, int budget, IEnumerable<Emotion>? emotions)
    {
        if (budget is < 0 or > MaxBudget) throw new DomainException("validation", $"Budget must be 0–{MaxBudget}.");
        var emotionList = (emotions ?? []).Distinct().ToList();
        if (emotionList.Count > 2) throw new DomainException("validation", "Choose up to two emotions.");

        return new GiftBrief(
            Guard.NotBlank(relationshipKey, nameof(relationshipKey), 40),
            Guard.NotBlank(occasionKey, nameof(occasionKey), 40),
            eventDate,
            Guard.NotBlank(ageRangeKey, nameof(ageRangeKey), 20),
            Guard.CleanList(interests, 40),
            Guard.CleanList(avoid, 40),
            string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()[..Math.Min(notes.Trim().Length, 500)],
            budget,
            emotionList);
    }

    public GiftBrief WithInterests(IEnumerable<string> extra) =>
        this with { Interests = Interests.Concat(extra).Distinct().Take(40).ToList() };

    public GiftBrief WithEmotions(IEnumerable<Emotion> emotions) =>
        this with { Emotions = emotions.Distinct().Take(2).ToList() };
}

public sealed record ClarifyingQuestion(string Key, string Text, IReadOnlyList<ClarifyingOption> Options);

public sealed record ClarifyingOption(string Key, string Label, string Hint);

public enum GiftSessionStatus
{
    AwaitingClarification = 1,
    Ready = 2
}

/// <summary>One "find me a gift" run. Holds the brief and, at most, one clarifying question.</summary>
public sealed class GiftSession : Entity
{
    public Guid UserId { get; private set; }
    public Guid? RecipientId { get; private set; }
    public GiftBrief Brief { get; private set; } = null!;
    public ClarifyingQuestion? Question { get; private set; }
    public string? Answer { get; private set; }
    public GiftSessionStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private GiftSession() { }

    public static GiftSession Start(Guid userId, Guid? recipientId, GiftBrief brief, DateTimeOffset now) =>
        new() { UserId = userId, RecipientId = recipientId, Brief = brief, CreatedAt = now, Status = GiftSessionStatus.Ready };

    public void Ask(ClarifyingQuestion question)
    {
        if (Question is not null) throw new DomainException("conflict", "A session asks at most one clarifying question.");
        Question = question;
        Status = GiftSessionStatus.AwaitingClarification;
    }

    /// <summary>Answer (or skip, with null) the clarifying question, optionally refining the brief.</summary>
    public void Resolve(string? answerKey, GiftBrief? refinedBrief)
    {
        if (Status != GiftSessionStatus.AwaitingClarification)
            throw new DomainException("conflict", "There is no open question.");
        if (answerKey is not null && Question!.Options.All(o => o.Key != answerKey))
            throw new DomainException("validation", "Unknown answer.");
        Answer = answerKey;
        if (refinedBrief is not null) Brief = refinedBrief;
        Status = GiftSessionStatus.Ready;
    }

    public void AttachRecipient(Guid recipientId) => RecipientId = recipientId;
}
