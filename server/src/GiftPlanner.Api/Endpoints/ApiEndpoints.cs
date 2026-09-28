using GiftPlanner.Api.Infrastructure;
using GiftPlanner.Application.Contracts;
using GiftPlanner.Application.Identity;
using GiftPlanner.Application.Orders;
using GiftPlanner.Application.Plans;
using GiftPlanner.Application.Recipients;
using GiftPlanner.Application.Recommendations;
using GiftPlanner.Application.Saved;
using GiftPlanner.Application.Sessions;
using GiftPlanner.Application.Taxonomy;

namespace GiftPlanner.Api.Endpoints;

public sealed record SaveSessionRecipientRequest(string Nickname);

/// <summary>
/// Minimal-API endpoint groups. Each group only translates HTTP ↔ use case; no business logic here.
/// Every authorized route is scoped to the caller inside the repositories, so ids in URLs can't leak data.
/// </summary>
public static class ApiEndpoints
{
    public const string LlmPolicy = "llm";

    public static void MapApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/v1");
        MapAuth(api);
        MapTaxonomy(api);

        var authed = api.MapGroup("").RequireAuthorization();
        MapSessions(authed);
        MapIdeas(authed);
        MapRecipients(authed);
        MapSaved(authed);
        MapPlans(authed);
        MapAccount(authed);
    }

    private static void MapAuth(RouteGroupBuilder api)
    {
        var g = api.MapGroup("/auth").WithTags("Auth");

        // Anonymous-first: the app gets a token on first launch, no sign-up wall.
        g.MapPost("/anonymous", async (IAuthService auth, CancellationToken ct) =>
            Results.Ok(await auth.CreateAnonymousAsync(ct)))
            .RequireRateLimiting("auth");

        // If called with an anonymous token, that user's data is merged into the signed-in account.
        g.MapPost("/sign-in", async (SignInRequest req, HttpContext http, IAuthService auth, CancellationToken ct) =>
            (await auth.SignInAsync(req, HttpCurrentUser.TryGetUserId(http.User), ct)).ToHttp())
            .RequireRateLimiting("auth");
    }

    private static void MapTaxonomy(RouteGroupBuilder api)
    {
        var g = api.MapGroup("/taxonomy").WithTags("Taxonomy").CacheOutput("taxonomy");
        g.MapGet("/relationships", (ITaxonomyService t) => Results.Ok(t.GetRelationships()));
        g.MapGet("/occasions", (ITaxonomyService t) => Results.Ok(t.GetOccasions()));
        g.MapGet("/age-ranges", (ITaxonomyService t) => Results.Ok(t.GetAgeRanges()));
        g.MapGet("/interests", (string? relationship, string? ageRange, ITaxonomyService t) =>
            Results.Ok(t.GetInterests(relationship, ageRange)));
    }

    private static void MapSessions(RouteGroupBuilder api)
    {
        var g = api.MapGroup("/gift-sessions").WithTags("Gift sessions");

        g.MapPost("/", async (CreateSessionRequest req, IGiftSessionService s, CancellationToken ct) =>
            (await s.CreateAsync(req, ct)).ToCreated(x => $"/api/v1/gift-sessions/{x.Id}"));

        g.MapPost("/{id:guid}/clarifications", async (Guid id, ClarifyRequest req, IGiftSessionService s, CancellationToken ct) =>
            (await s.ClarifyAsync(id, req, ct)).ToHttp());

        g.MapPost("/{id:guid}/recipient", async (Guid id, SaveSessionRecipientRequest req, IGiftSessionService s, CancellationToken ct) =>
            (await s.SaveRecipientAsync(id, req.Nickname, ct)).ToCreated(x => $"/api/v1/recipients/{x.Id}"));

        // Generating (and refining) ideas is the only route that may call the LLM → its own rate limit.
        g.MapPost("/{id:guid}/idea-sets", async (Guid id, GenerateIdeasRequest req, IIdeaService ideas, CancellationToken ct) =>
            (await ideas.GenerateAsync(id, req, ct)).ToCreated(x => $"/api/v1/idea-sets/{x.Id}"))
            .RequireRateLimiting(LlmPolicy);
    }

    private static void MapIdeas(RouteGroupBuilder api)
    {
        var g = api.MapGroup("").WithTags("Ideas");

        // Cursor paging feeds the client's virtual-scroll grid.
        g.MapGet("/idea-sets/{id:guid}/ideas", async (Guid id, string? cursor, int? limit, string? filter, IIdeaService ideas, CancellationToken ct) =>
            (await ideas.GetPageAsync(id, cursor, limit ?? IdeaService.DefaultPageSize, filter, ct)).ToHttp());

        g.MapGet("/refine-options", (IIdeaService ideas) => Results.Ok(ideas.GetRefineOptions()));

        g.MapGet("/ideas/{id:guid}", async (Guid id, IIdeaService ideas, CancellationToken ct) =>
            (await ideas.GetDetailAsync(id, ct)).ToHttp());

        g.MapPut("/ideas/{id:guid}/saved", async (Guid id, SaveIdeaRequest? req, ISavedService saved, CancellationToken ct) =>
            (await saved.SaveAsync(id, req ?? new SaveIdeaRequest(null), ct)).ToHttp());

        g.MapDelete("/ideas/{id:guid}/saved", async (Guid id, ISavedService saved, CancellationToken ct) =>
            (await saved.UnsaveAsync(id, ct)).ToHttp());

        g.MapPost("/ideas/{id:guid}/plan", async (Guid id, CreatePlanRequest? req, IPlanService plans, CancellationToken ct) =>
            (await plans.CreateAsync(id, req ?? new CreatePlanRequest(null), ct)).ToHttp());

        g.MapPost("/ideas/{id:guid}/purchase-intents", async (Guid id, CreatePurchaseRequest req, IPurchaseService purchases, CancellationToken ct) =>
            (await purchases.CreateIntentAsync(id, req, ct)).ToHttp());
    }

    private static void MapRecipients(RouteGroupBuilder api)
    {
        var g = api.MapGroup("/recipients").WithTags("My people");

        g.MapGet("/", async (IRecipientService r, CancellationToken ct) => Results.Ok(await r.ListAsync(ct)));
        g.MapGet("/{id:guid}", async (Guid id, IRecipientService r, CancellationToken ct) => (await r.GetAsync(id, ct)).ToHttp());
        g.MapPost("/", async (UpsertRecipientRequest req, IRecipientService r, CancellationToken ct) =>
            (await r.CreateAsync(req, ct)).ToCreated(x => $"/api/v1/recipients/{x.Id}"));
        g.MapPut("/{id:guid}", async (Guid id, UpsertRecipientRequest req, IRecipientService r, CancellationToken ct) =>
            (await r.UpdateAsync(id, req, ct)).ToHttp());
        g.MapDelete("/{id:guid}", async (Guid id, IRecipientService r, CancellationToken ct) => (await r.DeleteAsync(id, ct)).ToHttp());

        g.MapPost("/{id:guid}/occasions", async (Guid id, AddOccasionRequest req, IRecipientService r, CancellationToken ct) =>
            (await r.AddOccasionAsync(id, req, ct)).ToHttp());
        g.MapDelete("/{id:guid}/occasions/{occasionId:guid}", async (Guid id, Guid occasionId, IRecipientService r, CancellationToken ct) =>
            (await r.RemoveOccasionAsync(id, occasionId, ct)).ToHttp());
        g.MapPost("/{id:guid}/gift-history", async (Guid id, RecordGiftRequest req, IRecipientService r, CancellationToken ct) =>
            (await r.RecordGiftAsync(id, req, ct)).ToHttp());
    }

    private static void MapSaved(RouteGroupBuilder api) =>
        api.MapGet("/saved", async (Guid? recipientId, ISavedService saved, CancellationToken ct) =>
            Results.Ok(await saved.ListAsync(recipientId, ct))).WithTags("Saved");

    private static void MapPlans(RouteGroupBuilder api)
    {
        var g = api.MapGroup("/plans").WithTags("Plans");
        g.MapGet("/", async (IPlanService p, CancellationToken ct) => Results.Ok(await p.ListAsync(ct)));
        g.MapGet("/{id:guid}", async (Guid id, IPlanService p, CancellationToken ct) => (await p.GetAsync(id, ct)).ToHttp());
        g.MapPatch("/{id:guid}/tasks/{taskId:guid}", async (Guid id, Guid taskId, UpdateTaskRequest req, IPlanService p, CancellationToken ct) =>
            (await p.SetTaskDoneAsync(id, taskId, req.Done, ct)).ToHttp());
    }

    private static void MapAccount(RouteGroupBuilder api)
    {
        var g = api.MapGroup("/me").WithTags("Account");
        g.MapGet("/export", async (IAccountService a, CancellationToken ct) => Results.Ok(await a.ExportAsync(ct)));
        g.MapDelete("/", async (IAccountService a, CancellationToken ct) =>
        {
            await a.DeleteAsync(ct);
            return Results.NoContent();
        });
    }
}
