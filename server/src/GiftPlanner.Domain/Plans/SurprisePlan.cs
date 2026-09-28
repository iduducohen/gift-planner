using GiftPlanner.Domain.Common;

namespace GiftPlanner.Domain.Plans;

/// <summary>Turns an idea into an executable day: a timeline plus a checklist with due dates.</summary>
public sealed class SurprisePlan : Entity
{
    private readonly List<PlanStep> _steps = [];
    private readonly List<PlanTask> _tasks = [];

    public Guid UserId { get; private set; }
    public Guid RecommendationId { get; private set; }
    public Guid? RecipientId { get; private set; }
    public string Title { get; private set; } = "";
    public DateOnly EventDate { get; private set; }
    public int TotalMin { get; private set; }
    public int TotalMax { get; private set; }
    public string? PlanB { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyList<PlanStep> Steps => _steps;
    public IReadOnlyList<PlanTask> Tasks => _tasks;

    private SurprisePlan() { }

    public static SurprisePlan Create(Guid userId, Guid recommendationId, Guid? recipientId, string title, DateOnly eventDate,
        IEnumerable<PlanStep> steps, IEnumerable<PlanTask> tasks, string? planB, DateTimeOffset now)
    {
        var plan = new SurprisePlan
        {
            UserId = userId, RecommendationId = recommendationId, RecipientId = recipientId,
            Title = title, EventDate = eventDate, PlanB = planB, CreatedAt = now
        };
        plan._steps.AddRange(steps.OrderBy(s => s.Time));
        plan._tasks.AddRange(tasks.OrderBy(t => t.DueDate));
        // Totals are always computed in code from the steps, never taken from the LLM.
        plan.TotalMin = plan._steps.Sum(s => s.CostMin);
        plan.TotalMax = plan._steps.Sum(s => s.CostMax);
        return plan;
    }

    public PlanTask SetTaskDone(Guid taskId, bool done)
    {
        var task = _tasks.FirstOrDefault(t => t.Id == taskId)
                   ?? throw new DomainException("not_found", "Task not found.");
        task.SetDone(done);
        return task;
    }
}

public sealed class PlanStep
{
    public TimeOnly Time { get; private set; }
    public string Title { get; private set; } = "";
    public string Detail { get; private set; } = "";
    public int CostMin { get; private set; }
    public int CostMax { get; private set; }

    private PlanStep() { }

    public PlanStep(TimeOnly time, string title, string detail, int costMin, int costMax)
    {
        Time = time;
        Title = title;
        Detail = detail;
        CostMin = Math.Max(0, costMin);
        CostMax = Math.Max(CostMin, costMax);
    }
}

public sealed class PlanTask
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Title { get; private set; } = "";
    public DateOnly DueDate { get; private set; }
    public bool Done { get; private set; }

    private PlanTask() { }

    public PlanTask(string title, DateOnly dueDate)
    {
        Title = title;
        DueDate = dueDate;
    }

    internal void SetDone(bool done) => Done = done;
}
