using GiftPlanner.Domain.Common;

namespace GiftPlanner.Domain.Recipients;

/// <summary>
/// A person the user buys gifts for. Data minimisation: a nickname ("אשתי"), never a full name,
/// phone or address; an age range, never a birth year.
/// </summary>
public sealed class Recipient : Entity
{
    private readonly List<Occasion> _occasions = [];
    private readonly List<GiftHistoryEntry> _history = [];

    public Guid UserId { get; private set; }
    public string Nickname { get; private set; } = "";
    public string RelationshipKey { get; private set; } = "";
    public string AgeRangeKey { get; private set; } = "";
    public List<string> Interests { get; private set; } = [];
    public List<string> Avoid { get; private set; } = [];
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyList<Occasion> Occasions => _occasions;
    public IReadOnlyList<GiftHistoryEntry> History => _history;

    private Recipient() { }

    public static Recipient Create(Guid userId, string nickname, string relationshipKey, string ageRangeKey, DateTimeOffset now)
    {
        var r = new Recipient { UserId = userId, CreatedAt = now };
        r.Rename(nickname);
        r.SetBasics(relationshipKey, ageRangeKey);
        return r;
    }

    public void Rename(string nickname) => Nickname = Guard.NotBlank(nickname, nameof(nickname), 40);

    public void SetBasics(string relationshipKey, string ageRangeKey)
    {
        RelationshipKey = Guard.NotBlank(relationshipKey, nameof(relationshipKey), 40);
        AgeRangeKey = Guard.NotBlank(ageRangeKey, nameof(ageRangeKey), 20);
    }

    public void SetPreferences(IEnumerable<string>? interests, IEnumerable<string>? avoid, string? notes)
    {
        Interests = Guard.CleanList(interests, 40).ToList();
        Avoid = Guard.CleanList(avoid, 40).ToList();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()[..Math.Min(notes.Trim().Length, 500)];
    }

    public Occasion AddOccasion(string typeKey, int month, int day, bool recurring, int remindDaysBefore)
    {
        var occasion = new Occasion(typeKey, month, day, recurring, remindDaysBefore);
        _occasions.Add(occasion);
        return occasion;
    }

    public void RemoveOccasion(Guid occasionId) => _occasions.RemoveAll(o => o.Id == occasionId);

    public GiftHistoryEntry RecordGift(string title, DateOnly givenOn, GiftReaction? reaction)
    {
        var entry = new GiftHistoryEntry(Guard.NotBlank(title, nameof(title), 120), givenOn, reaction);
        _history.Add(entry);
        // A gift that should not be repeated goes straight into the avoid list, so it is never suggested again.
        if (reaction == GiftReaction.DontRepeat && !Avoid.Contains(entry.Title))
            Avoid.Add(entry.Title);
        return entry;
    }

    /// <summary>The next upcoming occasion from <paramref name="today"/>, if any.</summary>
    public (Occasion Occasion, DateOnly Date)? NextOccasion(DateOnly today) =>
        _occasions
            .Select(o => (Occasion: o, Date: o.NextDate(today)))
            .Where(x => x.Date.HasValue)
            .Select(x => (x.Occasion, Date: x.Date!.Value))
            .OrderBy(x => x.Date)
            .Cast<(Occasion, DateOnly)?>()
            .FirstOrDefault();
}

public sealed class Occasion
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string TypeKey { get; private set; } = "";
    public int Month { get; private set; }
    public int Day { get; private set; }
    public bool IsRecurring { get; private set; }
    public int RemindDaysBefore { get; private set; }

    private Occasion() { }

    public Occasion(string typeKey, int month, int day, bool recurring, int remindDaysBefore)
    {
        if (month is < 1 or > 12 || day < 1 || day > DateTime.DaysInMonth(2024, month))
            throw new DomainException("validation", "Invalid occasion date.");
        if (remindDaysBefore is < 0 or > 60)
            throw new DomainException("validation", "Reminder must be 0–60 days before.");
        TypeKey = Guard.NotBlank(typeKey, nameof(typeKey), 40);
        Month = month;
        Day = day;
        IsRecurring = recurring;
        RemindDaysBefore = remindDaysBefore;
    }

    /// <summary>Next calendar date of this occasion on or after <paramref name="today"/>. Feb 29 falls back to Feb 28.</summary>
    public DateOnly? NextDate(DateOnly today)
    {
        var thisYear = SafeDate(today.Year);
        if (thisYear >= today) return thisYear;
        return IsRecurring ? SafeDate(today.Year + 1) : null;
    }

    private DateOnly SafeDate(int year) => new(year, Month, Math.Min(Day, DateTime.DaysInMonth(year, Month)));
}

public sealed class GiftHistoryEntry
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Title { get; private set; } = "";
    public DateOnly GivenOn { get; private set; }
    public GiftReaction? Reaction { get; private set; }

    private GiftHistoryEntry() { }

    public GiftHistoryEntry(string title, DateOnly givenOn, GiftReaction? reaction)
    {
        Title = title;
        GivenOn = givenOn;
        Reaction = reaction;
    }
}
