namespace GiftPlanner.Domain.Common;

/// <summary>Base type for everything with an identity. Equality is by Id.</summary>
public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    public override bool Equals(object? obj) =>
        obj is Entity other && other.GetType() == GetType() && other.Id == Id;

    public override int GetHashCode() => Id.GetHashCode();
}

/// <summary>Thrown when a domain invariant is broken. Mapped to HTTP 400 by the API.</summary>
public sealed class DomainException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

internal static class Guard
{
    public static string NotBlank(string? value, string field, int maxLength = 200)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("validation", $"{field} is required.");
        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new DomainException("validation", $"{field} must be at most {maxLength} characters.");
        return trimmed;
    }

    public static IReadOnlyList<string> CleanList(IEnumerable<string>? values, int maxItems, int maxLength = 60) =>
        (values ?? [])
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v.Trim())
            .Where(v => v.Length <= maxLength)
            .Distinct(StringComparer.Ordinal)
            .Take(maxItems)
            .ToList();
}
