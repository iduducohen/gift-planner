using GiftPlanner.Application.Common;
using GiftPlanner.Application.Contracts;
using GiftPlanner.Domain.Users;

namespace GiftPlanner.Application.Identity;

public interface IAuthService
{
    Task<TokenDto> CreateAnonymousAsync(CancellationToken ct);
    Task<Result<TokenDto>> SignInAsync(SignInRequest request, Guid? currentUserId, CancellationToken ct);
}

/// <summary>
/// Use first, sign up later: every visitor gets an anonymous account. Signing in links that account
/// (or merges it into an existing one) so nothing created before sign-in is lost.
/// </summary>
public sealed class AuthService(
    IUserRepository users,
    IEnumerable<IExternalIdentityVerifier> verifiers,
    IUserDataMaintenance maintenance,
    ITokenIssuer tokens,
    IClock clock,
    IUnitOfWork uow) : IAuthService
{
    public async Task<TokenDto> CreateAnonymousAsync(CancellationToken ct)
    {
        var user = User.CreateAnonymous(clock.UtcNow);
        users.Add(user);
        await uow.SaveChangesAsync(ct);
        return ToDto(user);
    }

    public async Task<Result<TokenDto>> SignInAsync(SignInRequest request, Guid? currentUserId, CancellationToken ct)
    {
        var verifier = verifiers.FirstOrDefault(v => v.Provider.Equals(request.Provider, StringComparison.OrdinalIgnoreCase));
        if (verifier is null) return Error.Validation("Unsupported sign-in provider.");

        var identity = await verifier.VerifyAsync(request.IdToken, ct);
        if (identity is null) return new Error("invalid_token", "Sign-in failed.", ErrorType.Forbidden);

        var current = currentUserId is { } cid ? await users.GetAsync(cid, ct) : null;
        var existing = await users.FindByExternalAsync(identity.Provider, identity.ExternalId, ct);

        User signedIn;
        if (existing is not null)
        {
            if (current is not null && current.Id != existing.Id && current.IsAnonymous)
                await maintenance.MergeAsync(current.Id, existing.Id, ct);
            signedIn = existing;
        }
        else if (current is not null && current.IsAnonymous)
        {
            current.LinkExternal(identity.Provider, identity.ExternalId, identity.DisplayName);
            signedIn = current;
        }
        else
        {
            signedIn = User.CreateExternal(identity.Provider, identity.ExternalId, identity.DisplayName, clock.UtcNow);
            users.Add(signedIn);
        }

        signedIn.Touch(clock.UtcNow);
        await uow.SaveChangesAsync(ct);
        return ToDto(signedIn);
    }

    private TokenDto ToDto(User user) => new(tokens.Issue(user), user.Id, user.IsAnonymous, user.DisplayName);
}

public interface IAccountService
{
    Task<object> ExportAsync(CancellationToken ct);
    Task DeleteAsync(CancellationToken ct);
}

public sealed class AccountService(IUserDataMaintenance maintenance, ICurrentUser user) : IAccountService
{
    public Task<object> ExportAsync(CancellationToken ct) => maintenance.ExportAsync(user.UserId, ct);
    public Task DeleteAsync(CancellationToken ct) => maintenance.DeleteAllAsync(user.UserId, ct);
}
