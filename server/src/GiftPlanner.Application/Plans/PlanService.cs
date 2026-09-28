using GiftPlanner.Application.Common;
using GiftPlanner.Application.Contracts;
using GiftPlanner.Application.Taxonomy;
using GiftPlanner.Domain.Catalog;
using GiftPlanner.Domain.Common;
using GiftPlanner.Domain.GiftSessions;
using GiftPlanner.Domain.Ideas;
using GiftPlanner.Domain.Plans;

namespace GiftPlanner.Application.Plans;

public sealed record PlanInput(Recommendation Recommendation, CatalogIdea? CatalogIdea, GiftBrief Brief, DateOnly EventDate,
    DateOnly Today, string Title);

/// <summary>Turns an idea into a timeline and a checklist. Swappable: a template today, an LLM planner later.</summary>
public interface IPlanGenerator
{
    (IReadOnlyList<PlanStep> Steps, IReadOnlyList<PlanTask> Tasks, string? PlanB) Generate(PlanInput input);
}

/// <summary>
/// Deterministic plans per idea kind. Costs come from the recommendation's real price;
/// due dates respect the catalog lead time and never fall in the past.
/// </summary>
public sealed class TemplatePlanGenerator : IPlanGenerator
{
    public (IReadOnlyList<PlanStep> Steps, IReadOnlyList<PlanTask> Tasks, string? PlanB) Generate(PlanInput input)
    {
        var r = input.Recommendation;
        var leadDays = Math.Max(2, input.CatalogIdea?.LeadTimeDays ?? 7);
        var steps = new List<PlanStep>();
        var tasks = new List<PlanTask>();
        string? planB = null;

        DateOnly Due(int daysBefore)
        {
            var d = input.EventDate.AddDays(-daysBefore);
            return d < input.Today ? input.Today : d;
        }

        switch (r.Kind)
        {
            case IdeaKind.Bundle:
                steps.Add(new PlanStep(new TimeOnly(10, 0), "פרחים מגיעים הביתה", "עם פתק קצר: ״תשמרי את הערב פנוי״", 150, 150));
                steps.Add(new PlanStep(new TimeOnly(14, 0), "הודעת הפתעה", "רמז קטן למה שמחכה בערב", 0, 0));
                steps.Add(new PlanStep(new TimeOnly(18, 0), r.Title, "הרגע המרכזי של היום", Math.Max(0, r.PriceMin - 150), Math.Max(0, r.PriceMax - 150)));
                steps.Add(new PlanStep(new TimeOnly(20, 30), "הברכה שלך", "כתוב אותה מראש", 0, 0));
                tasks.Add(new PlanTask($"להזמין: {r.Title}", Due(leadDays)));
                tasks.Add(new PlanTask("להזמין פרחים למשלוח", Due(3)));
                break;

            case IdeaKind.Experience:
                steps.Add(new PlanStep(new TimeOnly(14, 0), "הודעת הפתעה", "״תתפנה/י לערב, אני אוסף/ת אותך״", 0, 0));
                steps.Add(new PlanStep(new TimeOnly(17, 30), "איסוף, בלי לגלות לאן", "החלק הכי כיפי הוא הדרך", 0, 0));
                steps.Add(new PlanStep(new TimeOnly(18, 0), r.Title, "החוויה עצמה", r.PriceMin, r.PriceMax));
                steps.Add(new PlanStep(new TimeOnly(20, 30), "הברכה שלך", "כתוב אותה מראש", 0, 0));
                tasks.Add(new PlanTask($"להזמין: {r.Title}", Due(leadDays)));
                tasks.Add(new PlanTask("לתאם הסעה ואיסוף", Due(2)));
                tasks.Add(new PlanTask("לבדוק תחזית ושעות פתיחה", Due(1)));
                planB = "אם מזג האוויר לא משתף פעולה: להעביר את החוויה הביתה עם אותה אווירה, ולקבוע תאריך חדש.";
                break;

            case IdeaKind.Pamper:
                steps.Add(new PlanStep(new TimeOnly(8, 30), "בוקר עם הודעה אישית", "קפה למיטה ופתק", 0, 0));
                steps.Add(new PlanStep(new TimeOnly(10, 0), r.Title, "זמן פינוק", r.PriceMin, r.PriceMax));
                steps.Add(new PlanStep(new TimeOnly(19, 30), "הברכה שלך", "בלי מסכים, רק אתם", 0, 0));
                tasks.Add(new PlanTask($"להזמין: {r.Title}", Due(leadDays)));
                break;

            default: // Product, Personal
                steps.Add(new PlanStep(new TimeOnly(19, 0), "ערב רגיל לכאורה", "שלא תחשוד שמשהו מתוכנן", 0, 0));
                steps.Add(new PlanStep(new TimeOnly(20, 0), r.Title, "להגיש עם הברכה", r.PriceMin, r.PriceMax));
                tasks.Add(new PlanTask($"להזמין: {r.Title}", Due(leadDays)));
                tasks.Add(new PlanTask("לעטוף ולהסתיר", Due(1)));
                break;
        }

        tasks.Add(new PlanTask("לכתוב ברכה אישית", Due(1)));
        return (steps, tasks, planB);
    }
}

public interface IPlanService
{
    Task<Result<PlanDto>> CreateAsync(Guid recommendationId, CreatePlanRequest request, CancellationToken ct);
    Task<IReadOnlyList<PlanDto>> ListAsync(CancellationToken ct);
    Task<Result<PlanDto>> GetAsync(Guid id, CancellationToken ct);
    Task<Result<PlanDto>> SetTaskDoneAsync(Guid planId, Guid taskId, bool done, CancellationToken ct);
}

public sealed class PlanService(
    IPlanRepository plans,
    IIdeaSetRepository sets,
    IGiftSessionRepository sessions,
    IRecipientRepository recipients,
    ICatalogReader catalog,
    IPlanGenerator generator,
    ITaxonomyService taxonomy,
    ICurrentUser user,
    IClock clock,
    IUnitOfWork uow) : IPlanService
{
    /// <summary>Idempotent: asking again for the same idea returns the existing plan.</summary>
    public async Task<Result<PlanDto>> CreateAsync(Guid recommendationId, CreatePlanRequest request, CancellationToken ct)
    {
        if (await plans.FindByRecommendationAsync(recommendationId, user.UserId, ct) is { } existing)
            return existing.ToDto(clock.Today);

        var rec = await sets.GetRecommendationAsync(recommendationId, user.UserId, ct);
        if (rec is null) return Error.NotFound("Idea");
        var set = await sets.GetAsync(rec.IdeaSetId, user.UserId, ct);
        var session = set is null ? null : await sessions.GetAsync(set.SessionId, user.UserId, ct);
        if (session is null) return Error.NotFound("Session");

        var eventDate = request.EventDate ?? session.Brief.EventDate ?? clock.Today.AddDays(7);
        if (eventDate < clock.Today) return Error.Validation("Event date is in the past.");

        var who = session.RecipientId is { } rid && await recipients.GetAsync(rid, user.UserId, ct) is { } recipient
            ? recipient.Nickname
            : taxonomy.RelationshipLabel(session.Brief.RelationshipKey);
        var title = $"{taxonomy.OccasionLabel(session.Brief.OccasionKey)} · {who}";

        var idea = (await catalog.GetActiveIdeasAsync(ct)).FirstOrDefault(i => i.Id == rec.CatalogIdeaId);
        var (steps, tasks, planB) = generator.Generate(new PlanInput(rec, idea, session.Brief, eventDate, clock.Today, title));

        var plan = SurprisePlan.Create(user.UserId, rec.Id, session.RecipientId, title, eventDate, steps, tasks, planB, clock.UtcNow);
        plans.Add(plan);
        await uow.SaveChangesAsync(ct);
        return plan.ToDto(clock.Today);
    }

    public async Task<IReadOnlyList<PlanDto>> ListAsync(CancellationToken ct) =>
        (await plans.ListAsync(user.UserId, ct)).OrderBy(p => p.EventDate).Select(p => p.ToDto(clock.Today)).ToList();

    public async Task<Result<PlanDto>> GetAsync(Guid id, CancellationToken ct) =>
        await plans.GetAsync(id, user.UserId, ct) is { } p ? p.ToDto(clock.Today) : Error.NotFound("Plan");

    public async Task<Result<PlanDto>> SetTaskDoneAsync(Guid planId, Guid taskId, bool done, CancellationToken ct)
    {
        var plan = await plans.GetAsync(planId, user.UserId, ct);
        if (plan is null) return Error.NotFound("Plan");
        plan.SetTaskDone(taskId, done);
        await uow.SaveChangesAsync(ct);
        return plan.ToDto(clock.Today);
    }
}
