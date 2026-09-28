namespace GiftPlanner.Domain.Common;

/// <summary>What the giver wants the recipient to feel. The product's core differentiator.</summary>
public enum Emotion
{
    Moved = 1,      // שתתרגש
    Surprised = 2,  // שתופתע
    Pampered = 3,   // מפונקת
    Thrilled = 4,   // שתתלהב
    Laugh = 5,      // שתצחק
    Adventure = 6   // הרפתקה
}

public enum IdeaKind
{
    Experience = 1,
    Product = 2,
    Personal = 3,
    Pamper = 4,
    Bundle = 5
}

/// <summary>Who an idea or interest is appropriate for.</summary>
public enum Audience
{
    Kid = 1,
    Teen = 2,
    Adult = 3
}

/// <summary>Where a price came from. The UI must always show this; the AI never invents prices.</summary>
public enum PriceSource
{
    CatalogEstimate = 1,
    ProviderListed = 2,
    ProviderLive = 3
}

public enum GiftReaction
{
    Loved = 1,
    Liked = 2,
    Meh = 3,
    DontRepeat = 4
}
