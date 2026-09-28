using GiftPlanner.Domain.Common;

namespace GiftPlanner.Domain.Users;

/// <summary>
/// An app user. Starts anonymous (device token) and is upgraded in place when the user signs in,
/// so sessions, recipients and saved ideas created before sign-in are kept.
/// </summary>
public sealed class User : Entity
{
    public string? ExternalProvider { get; private set; }
    public string? ExternalId { get; private set; }
    public string? DisplayName { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset LastSeenAt { get; private set; }

    public bool IsAnonymous => ExternalId is null;

    private User() { }

    public static User CreateAnonymous(DateTimeOffset now) => new() { CreatedAt = now, LastSeenAt = now };

    public static User CreateExternal(string provider, string externalId, string? displayName, DateTimeOffset now)
    {
        var user = CreateAnonymous(now);
        user.LinkExternal(provider, externalId, displayName);
        return user;
    }

    public void LinkExternal(string provider, string externalId, string? displayName)
    {
        ExternalProvider = Guard.NotBlank(provider, nameof(provider), 20);
        ExternalId = Guard.NotBlank(externalId, nameof(externalId), 200);
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
    }

    public void Touch(DateTimeOffset now) => LastSeenAt = now;
}
