using GiftPlanner.Application.Common;
using GiftPlanner.Application.Contracts;
using GiftPlanner.Application.Plans;
using GiftPlanner.Application.Recommendations;
using GiftPlanner.Domain.Orders;

namespace GiftPlanner.Application.Orders;

public interface IPurchaseService
{
    Task<Result<PurchaseResultDto>> CreateIntentAsync(Guid recommendationId, CreatePurchaseRequest request, CancellationToken ct);
}

/// <summary>
/// The business model in code: the user buys at the partner's discounted price, the partner gets a tracked lead,
/// and the idea becomes a plan so the user is guided until the event.
/// </summary>
public sealed class PurchaseService(
    IIdeaSetRepository sets,
    IOfferResolver offers,
    IPurchaseIntentRepository intents,
    IPlanService plans,
    ICurrentUser user,
    IClock clock,
    IUnitOfWork uow) : IPurchaseService
{
    public async Task<Result<PurchaseResultDto>> CreateIntentAsync(Guid recommendationId, CreatePurchaseRequest request, CancellationToken ct)
    {
        var channel = request.Channel switch
        {
            "partner" => PurchaseChannel.PartnerSite,
            "in_app" => PurchaseChannel.InApp,
            _ => (PurchaseChannel?)null
        };
        if (channel is null) return Error.Validation("Channel must be 'partner' or 'in_app'.");
        if (request.ForDate is { } d && d < clock.Today) return Error.Validation("Date is in the past.");

        var rec = await sets.GetRecommendationAsync(recommendationId, user.UserId, ct);
        if (rec is null) return Error.NotFound("Idea");

        var quote = (await offers.BestOffersAsync([rec.CatalogIdeaId], ct)).GetValueOrDefault(rec.CatalogIdeaId);
        if (quote is null)
            return new Error("no_offer", "עדיין אין עסק שותף לרעיון הזה. אפשר לשמור אותו ולהפוך לתוכנית.", ErrorType.Conflict);

        var intent = PurchaseIntent.Create(user.UserId, rec.Id, quote.OfferId, quote.PartnerId, request.ForDate,
            channel.Value, quote.CouponCode, quote.FinalPrice, clock.UtcNow);
        intents.Add(intent);
        await uow.SaveChangesAsync(ct);
        Telemetry.PurchaseIntents.Add(1);

        var plan = await plans.CreateAsync(rec.Id, new CreatePlanRequest(request.ForDate), ct);
        if (!plan.IsSuccess) return plan.Error!;

        var redirect = channel == PurchaseChannel.PartnerSite ? BuildRedirect(quote.Url, quote.CouponCode, intent.Id) : null;
        return new PurchaseResultDto(intent.Id, request.Channel, intent.Status.ToString(), redirect, quote.CouponCode,
            quote.FinalPrice, plan.Value.Id);
    }

    /// <summary>Tracked link: the partner can attribute the sale and apply the coupon automatically.</summary>
    internal static string BuildRedirect(string url, string coupon, Guid intentId)
    {
        var sep = url.Contains('?') ? '&' : '?';
        return $"{url}{sep}coupon={Uri.EscapeDataString(coupon)}&ref={intentId:N}&utm_source=giftplanner&utm_medium=app";
    }
}
