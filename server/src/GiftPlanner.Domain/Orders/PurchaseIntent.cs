using GiftPlanner.Domain.Common;

namespace GiftPlanner.Domain.Orders;

public enum PurchaseChannel
{
    PartnerSite = 1,
    InApp = 2
}

public enum PurchaseStatus
{
    RedirectedToPartner = 1,
    PendingPartnerConfirmation = 2,
    Confirmed = 3,
    Cancelled = 4
}

/// <summary>
/// Records that a user chose to buy an idea through a partner. This is the core of the business model:
/// the partner gives our users a discount and receives tracked demand.
/// </summary>
public sealed class PurchaseIntent : Entity
{
    public Guid UserId { get; private set; }
    public Guid RecommendationId { get; private set; }
    public Guid OfferId { get; private set; }
    public Guid PartnerId { get; private set; }
    public DateOnly? ForDate { get; private set; }
    public PurchaseChannel Channel { get; private set; }
    public PurchaseStatus Status { get; private set; }
    public string CouponCode { get; private set; } = "";
    public int PriceAtIntent { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private PurchaseIntent() { }

    public static PurchaseIntent Create(Guid userId, Guid recommendationId, Guid offerId, Guid partnerId, DateOnly? forDate,
        PurchaseChannel channel, string couponCode, int price, DateTimeOffset now) =>
        new()
        {
            UserId = userId, RecommendationId = recommendationId, OfferId = offerId, PartnerId = partnerId,
            ForDate = forDate, Channel = channel, CouponCode = couponCode, PriceAtIntent = price, CreatedAt = now,
            Status = channel == PurchaseChannel.PartnerSite ? PurchaseStatus.RedirectedToPartner : PurchaseStatus.PendingPartnerConfirmation
        };

    public void Confirm() => Status = PurchaseStatus.Confirmed;
    public void Cancel() => Status = PurchaseStatus.Cancelled;
}
