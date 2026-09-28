namespace GiftPlanner.Application.Contracts;

// ---- Auth ----
public sealed record TokenDto(string Token, Guid UserId, bool IsAnonymous, string? DisplayName);
public sealed record SignInRequest(string Provider, string IdToken);

// ---- Sessions ----
public sealed record CreateSessionRequest(
    Guid? RecipientId,
    string RelationshipKey,
    string OccasionKey,
    DateOnly? EventDate,
    string AgeRangeKey,
    List<string>? Interests,
    List<string>? Avoid,
    string? Notes,
    int Budget,
    List<string>? Emotions);

public sealed record ClarifyRequest(string? AnswerKey);
public sealed record QuestionOptionDto(string Key, string Label, string Hint);
public sealed record QuestionDto(string Key, string Text, IReadOnlyList<QuestionOptionDto> Options);
public sealed record SessionDto(Guid Id, string Status, QuestionDto? Question);

// ---- Ideas ----
public sealed record GenerateIdeasRequest(Guid? ParentSetId, string? RefinePreset, string? RefineText);

public sealed record IdeaDto(
    Guid Id,
    int Rank,
    bool IsTop,
    string Title,
    string Kind,
    string KindLabel,
    string Why,
    string? EmotionalValue,
    string? HowTo,
    string? Upgrade,
    int PriceMin,
    int PriceMax,
    int? ListPrice,
    int? DiscountPercent,
    string PriceSource,
    int Surprise,
    int Effort,
    string IconKey,
    bool IsAiWritten,
    bool Saved);

public sealed record IdeaPageDto(IReadOnlyList<IdeaDto> Items, string? NextCursor, int Total);

public sealed record IdeaSetDto(Guid Id, Guid SessionId, string? RefineLabel, int Total, IReadOnlyList<IdeaDto> Top, IdeaPageDto More);

public sealed record OfferDto(
    Guid Id, string PartnerName, string PartnerRegion, bool PartnerVerified,
    int ListPrice, int DiscountPercent, int FinalPrice, string PriceSource, DateOnly VerifiedAt);

public sealed record IdeaDetailDto(IdeaDto Idea, OfferDto? Offer, IReadOnlyList<string> Includes, DateOnly? CatalogVerifiedAt);

public sealed record RefineOptionDto(string Key, string Label);

// ---- Recipients ----
public sealed record UpsertRecipientRequest(
    string Nickname, string RelationshipKey, string AgeRangeKey,
    List<string>? Interests, List<string>? Avoid, string? Notes);

public sealed record AddOccasionRequest(string TypeKey, int Month, int Day, bool Recurring = true, int RemindDaysBefore = 10);
public sealed record RecordGiftRequest(string Title, DateOnly GivenOn, string? Reaction);

public sealed record NextOccasionDto(Guid Id, string TypeKey, string Label, DateOnly Date, int DaysLeft);
public sealed record OccasionDto(Guid Id, string TypeKey, string Label, int Month, int Day, bool Recurring, int RemindDaysBefore);
public sealed record GiftHistoryDto(Guid Id, string Title, DateOnly GivenOn, string? Reaction);

public sealed record RecipientSummaryDto(
    Guid Id, string Nickname, string RelationshipKey, string RelationshipLabel, string AgeRangeKey, NextOccasionDto? Next);

public sealed record RecipientDto(
    Guid Id, string Nickname, string RelationshipKey, string RelationshipLabel, string AgeRangeKey,
    IReadOnlyList<string> Interests, IReadOnlyList<string> Avoid, string? Notes,
    IReadOnlyList<OccasionDto> Occasions, IReadOnlyList<GiftHistoryDto> History, NextOccasionDto? Next);

// ---- Saved ----
public sealed record SaveIdeaRequest(Guid? RecipientId);
public sealed record SavedIdeaDto(IdeaDto Idea, Guid? RecipientId, string? RecipientNickname, DateTimeOffset SavedAt);

// ---- Plans ----
public sealed record CreatePlanRequest(DateOnly? EventDate);
public sealed record UpdateTaskRequest(bool Done);
public sealed record PlanStepDto(string Time, string Title, string Detail, int CostMin, int CostMax);
public sealed record PlanTaskDto(Guid Id, string Title, DateOnly DueDate, bool Done);

public sealed record PlanDto(
    Guid Id, Guid RecommendationId, Guid? RecipientId, string Title, DateOnly EventDate, int DaysLeft,
    int TotalMin, int TotalMax, string? PlanB, IReadOnlyList<PlanStepDto> Steps, IReadOnlyList<PlanTaskDto> Tasks);

// ---- Purchases ----
public sealed record CreatePurchaseRequest(DateOnly? ForDate, string Channel);
public sealed record PurchaseResultDto(Guid IntentId, string Channel, string Status, string? RedirectUrl, string CouponCode, int Price, Guid PlanId);
