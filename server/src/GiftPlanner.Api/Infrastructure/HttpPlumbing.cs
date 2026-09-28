using System.Security.Claims;
using GiftPlanner.Application.Common;
using GiftPlanner.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace GiftPlanner.Api.Infrastructure;

/// <summary>ICurrentUser from the validated JWT. Endpoints that use it always require authorization.</summary>
internal sealed class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal Principal => accessor.HttpContext?.User ?? new ClaimsPrincipal();

    public Guid UserId => TryGetUserId(Principal) ?? throw new UnauthorizedAccessException("No authenticated user.");

    public bool IsAnonymous => Principal.FindFirst("anon")?.Value != "false";

    public static Guid? TryGetUserId(ClaimsPrincipal principal) =>
        Guid.TryParse(principal.FindFirst("sub")?.Value, out var id) ? id : null;
}

/// <summary>Result → HTTP. Keeps endpoints one line long and the error format uniform (RFC 9457 problem details).</summary>
internal static class ResultHttpExtensions
{
    public static IResult ToHttp(this Result result) =>
        result.IsSuccess ? Results.NoContent() : Problem(result.Error!);

    public static IResult ToHttp<T>(this Result<T> result) =>
        result.IsSuccess ? Results.Ok(result.Value) : Problem(result.Error!);

    public static IResult ToCreated<T>(this Result<T> result, Func<T, string> location) =>
        result.IsSuccess ? Results.Created(location(result.Value), result.Value) : Problem(result.Error!);

    public static IResult Problem(Error error) => Results.Problem(
        statusCode: StatusFor(error.Type),
        title: error.Message,
        type: $"https://gift-planner.app/errors/{error.Code}",
        extensions: new Dictionary<string, object?> { ["code"] = error.Code });

    private static int StatusFor(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.Unavailable => StatusCodes.Status503ServiceUnavailable,
        _ => StatusCodes.Status500InternalServerError
    };
}

/// <summary>Domain invariants that escape a use case become 400s; everything else a 500 without internals.</summary>
internal sealed class DomainExceptionHandler(IProblemDetailsService problems, ILogger<DomainExceptionHandler> log) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception exception, CancellationToken ct)
    {
        var (status, title, code) = exception switch
        {
            DomainException d => (StatusCodes.Status400BadRequest, d.Message, d.Code),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Authentication required.", "unauthorized"),
            BadHttpRequestException => (StatusCodes.Status400BadRequest, "Malformed request.", "bad_request"),
            _ => (StatusCodes.Status500InternalServerError, "Something went wrong. Please try again.", "internal")
        };
        if (status == StatusCodes.Status500InternalServerError) log.LogError(exception, "Unhandled exception");

        http.Response.StatusCode = status;
        return await problems.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Type = $"https://gift-planner.app/errors/{code}",
                Extensions = { ["code"] = code, ["traceId"] = http.TraceIdentifier }
            }
        });
    }
}
