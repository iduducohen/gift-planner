using System.Security.Cryptography;
using System.Text;
using GiftPlanner.Domain.Catalog;
using GiftPlanner.Domain.Common;
using static GiftPlanner.Domain.Common.Emotion;

namespace GiftPlanner.Infrastructure.Persistence;

/// <summary>
/// Demo catalog for development. Real ideas are curated by a content person and imported;
/// partner names are descriptive placeholders and every URL points at example.com.
/// Interests must use the exact labels from <c>TaxonomyData.Interests</c>.
/// </summary>
internal static class CatalogSeed
{
    private static readonly DateOnly Verified = new(2026, 9, 1);
    private static readonly Audience[] A = [Audience.Adult];
    private static readonly Audience[] AT = [Audience.Adult, Audience.Teen];
    private static readonly Audience[] T = [Audience.Teen];
    private static readonly Audience[] K = [Audience.Kid];
    private static readonly string[] Partners = ["partner_f", "partner_m", "fiancee", "fiance"];
    private static readonly string[] Family = ["mom", "dad", "grandma", "grandpa", "mother_in_law", "father_in_law", "aunt", "uncle", "sister", "brother"];
    private static readonly string[] Close = [.. Partners, .. Family, "daughter", "son", "friend_f", "friend_m", "childhood_friend"];
    private static readonly string[] Any = [];

    public static Guid IdFor(string kind, string name) => new(MD5.HashData(Encoding.UTF8.GetBytes($"{kind}:{name}")));

    private static CatalogIdea I(string title, string desc, IdeaKind kind, string[] interests, Emotion[] emotions, Audience[] aud,
        string[] rels, string[] occ, int min, int max, int surprise, int effort, int novelty, int lead, string icon,
        string? upgrade = null, string[]? includes = null) =>
        CatalogIdea.Create(IdFor("idea", title), title, desc, kind, interests, emotions, rels, aud, occ,
            min, max, surprise, effort, novelty, lead, icon, upgrade, includes ?? [], Verified);

    public static IReadOnlyList<CatalogIdea> Ideas() =>
    [
        I("פיקניק שקיעה אישי", "פינה מעוצבת מול השקיעה, אוכל טוב וברכה ממך.", IdeaKind.Experience,
            ["שקיעות", "טבע וציפורים", "צילום", "יין"], [Moved, Surprised], A, Partners, Any, 550, 700, 5, 3, 4, 3, "sunset",
            "צלם ל-30 דקות, כדי שהפעם היא תהיה בתמונה", ["מקום עם נוף לשקיעה", "עיצוב, שמיכות ותאורה", "מגש אירוח ויין", "זר פרחים קטן"]),
        I("יום הולדת בשלושה רגעים", "פרחים בבוקר, מתנה קטנה בצהריים, ארוחה בערב.", IdeaKind.Bundle,
            ["פרחים", "מסעדות שף", "תכשיטים"], [Moved, Surprised], A, Partners, ["birthday", "milestone_birthday"], 650, 750, 4, 3, 3, 5, "gift",
            null, ["זר פרחים עם משלוח", "מתנה אישית קטנה", "ארוחת ערב זוגית"]),
        I("אלבום מהטיולים שלכם", "התמונות שצולמו בטיולים, סוף סוף על נייר, עם כיתוב קצר ממך.", IdeaKind.Personal,
            ["צילום", "טיולים בארץ", "טיולים בחו״ל"], [Moved], A, Close, Any, 250, 400, 3, 4, 3, 7, "frame",
            "כריכת בד עם הקדשה מוטבעת", ["עד 40 עמודים", "עיצוב אישי", "משלוח עד הבית"]),
        I("בוקר ספא זוגי + פרחים בחדר", "הפינוק האהוב, עם תוספת שמחכה כשחוזרים.", IdeaKind.Pamper,
            ["ספא", "עיסויים", "פרחים"], [Pampered], A, Partners, Any, 550, 700, 3, 1, 2, 3, "spa",
            null, ["עיסוי זוגי 50 דקות", "בריכה וג׳קוזי", "ארוחת בוקר קלה"]),
        I("סדנת צילום שקיעה עם צלם", "שעתיים עם צלם מקצועי במקום הכי יפה בשקיעה.", IdeaKind.Experience,
            ["צילום", "שקיעות", "קורסים ולמידה"], [Thrilled, Surprised], A, Any, Any, 400, 550, 5, 2, 5, 5, "camera"),
        I("סדנת קרמיקה זוגית", "שעתיים ידיים בחימר, ושתי כוסות שייצאו מזה.", IdeaKind.Experience,
            ["קרמיקה", "DIY", "ציור"], [Laugh, Moved], A, Any, Any, 380, 480, 3, 2, 4, 4, "cup"),
        I("ארוחת טעימות אצל שף", "תפריט טעימות של שף, החוויה שבקצה של מי שאוהב מסעדות.", IdeaKind.Experience,
            ["מסעדות שף", "יין"], [Pampered, Thrilled], A, Any, Any, 550, 800, 3, 1, 3, 5, "plate"),
        I("זר עונתי + שוקולד בוטיק", "קלאסי, אבל מהמקום הכי טוב.", IdeaKind.Product,
            ["פרחים", "שוקולד", "קינוחים"], [Moved], A, Any, Any, 200, 300, 2, 1, 1, 1, "flower"),
        I("הדפסת צילום אהוב על קנבס", "הצילום הכי יפה, תלוי על הקיר.", IdeaKind.Personal,
            ["צילום", "עיצוב הבית"], [Moved], A, Any, Any, 180, 280, 3, 2, 3, 5, "frame"),
        I("סיור טעימות ביקב", "חצי יום בין הכרמים, יין טוב וגבינות.", IdeaKind.Experience,
            ["יין", "טיולים בארץ", "גבינות"], [Pampered, Adventure], A, Any, Any, 300, 450, 3, 1, 3, 3, "wine"),
        I("קופסת ספא לבית", "פינוק שאפשר לפתוח מתי שבא.", IdeaKind.Pamper,
            ["ספא", "טיפוח פנים", "נרות וריחות"], [Pampered], A, Any, Any, 220, 320, 2, 1, 2, 2, "spa"),
        I("ערב סטנדאפ + ארוחה", "ערב של צחוק, בדיוק מה שצריך.", IdeaKind.Experience,
            ["סטנדאפ", "מסעדות שכונתיות"], [Laugh], A, Any, Any, 400, 550, 3, 1, 3, 3, "mic"),
        I("עגילים בעיצוב אישי", "תכשיט שעוד אין, בעיצוב שמתאים בדיוק.", IdeaKind.Personal,
            ["תכשיטים"], [Moved], A, Close, Any, 350, 450, 3, 2, 3, 10, "ring"),
        I("לילה במלון בוטיק בעיר", "בריחה קטנה בלי לנסוע רחוק.", IdeaKind.Experience,
            ["מלונות בוטיק", "סופ״ש בצימר"], [Pampered, Surprised], A, Partners, Any, 650, 900, 4, 2, 3, 5, "bed"),
        I("שייט שקיעה ביפו", "שקיעה מהים, זווית שעוד לא ראיתם.", IdeaKind.Experience,
            ["ים וחופים", "שקיעות"], [Moved, Adventure], A, Any, Any, 500, 700, 4, 1, 4, 3, "boat"),
        I("כרטיסים להופעה", "ערב של מוזיקה שאוהבים, יחד.", IdeaKind.Experience,
            ["הופעות", "מוזיקה ישראלית", "רוק", "ג׳אז"], [Thrilled], AT, Any, Any, 300, 600, 3, 1, 2, 2, "ticket"),
        I("מנוי לשיעורי יוגה", "רגע של שקט בכל שבוע.", IdeaKind.Pamper,
            ["יוגה", "פילאטיס", "מדיטציה"], [Pampered], A, Any, Any, 350, 500, 2, 1, 2, 2, "heart"),
        I("ערכת נרות ובישום לבית", "אווירה בבית, כל ערב.", IdeaKind.Product,
            ["נרות וריחות", "עיצוב הבית"], [Pampered], A, Any, Any, 180, 260, 1, 1, 1, 2, "candle"),
        I("אלבום ״365 סיבות״ בכתב ידך", "הכי אישי שיש. עולה מעט, שווה המון.", IdeaKind.Personal,
            ["כתיבה", "זמן משפחה"], [Moved], A, Close, Any, 80, 150, 4, 5, 4, 7, "book"),
        I("טיול ג׳יפים במדבר", "הרפתקה, שקיעה במכתש ותה על האש.", IdeaKind.Experience,
            ["שטח וג׳יפים", "מדבר", "שקיעות"], [Adventure, Thrilled], A, Any, Any, 500, 750, 4, 2, 4, 5, "jeep"),
        I("תיק צילום מעוצב", "לציוד שנסחב לכל טיול.", IdeaKind.Product,
            ["צילום", "מצלמות", "טיולים בחו״ל"], [Thrilled], A, Any, Any, 300, 450, 2, 1, 2, 3, "bag"),
        I("סרטון ברכות מחברים ומשפחה", "כולם מברכים בסרטון אחד. אתה רק מרכז.", IdeaKind.Personal,
            ["זמן משפחה", "ערבי חברים"], [Moved, Surprised], A, Any, Any, 0, 120, 5, 4, 4, 10, "mic"),
        I("מצלמת פילם מיידית", "תמונות שמחזיקים ביד, ברגע.", IdeaKind.Product,
            ["צילום", "מצלמות", "מסיבות"], [Thrilled, Laugh], AT, Any, Any, 350, 500, 3, 1, 3, 3, "camera"),
        I("סדנת בישול איטלקי זוגית", "מבשלים יחד, אוכלים יחד.", IdeaKind.Experience,
            ["בישול", "אוכל איטלקי"], [Laugh, Moved], A, Any, Any, 450, 600, 3, 1, 3, 4, "plate"),
        I("ספר מתכונים משפחתי מעוצב", "המתכונים של המשפחה, בספר אחד שיעבור לנכדים.", IdeaKind.Personal,
            ["בישול", "אפייה", "זמן משפחה"], [Moved], A, Family, Any, 200, 320, 4, 4, 4, 14, "book"),
        I("ארוחת בוקר מפנקת עד הבית", "מתחילים את היום הנכון, בלי לצאת מהמיטה.", IdeaKind.Product,
            ["ארוחות בוקר", "קפה"], [Pampered, Surprised], A, Any, Any, 160, 240, 4, 1, 2, 2, "coffee"),
        I("סדנת וויסקי וטעימות", "ערב טעימות עם מומחה, ובקבוק הביתה.", IdeaKind.Experience,
            ["וויסקי", "קוקטיילים"], [Thrilled], A, Any, Any, 250, 400, 3, 1, 3, 4, "wine"),
        I("כרטיסים למשחק + ארוחה", "משחק מהיציע וארוחה לפני. יום שלם של כיף.", IdeaKind.Experience,
            ["צפייה בכדורגל", "צפייה בכדורסל", "על האש"], [Thrilled, Laugh], AT, Any, Any, 300, 600, 3, 1, 2, 3, "ball"),
        I("סט על האש מקצועי", "כלים של מקצוענים למנגל של שישי.", IdeaKind.Product,
            ["על האש", "בישול"], [Thrilled], A, Any, Any, 250, 450, 2, 1, 2, 3, "plate"),
        I("אוזניות סינון רעשים", "שקט מוחלט, וסאונד מעולה.", IdeaKind.Product,
            ["אוזניות וסאונד", "מוזיקה ישראלית", "גאדג׳טים", "פודקאסטים"], [Thrilled], AT, Any, Any, 400, 1200, 2, 1, 1, 2, "headphones"),
        I("חדר בריחה לחבורה", "שעה של צחוק, לחץ וצוות אחד.", IdeaKind.Experience,
            ["חדרי בריחה", "משחקי קופסה", "ערבי חברים"], [Laugh, Adventure], AT, Any, Any, 250, 450, 3, 2, 3, 3, "puzzle"),
        I("שיעור פאדל פרטי", "שיעור עם מאמן, גם למתחילים.", IdeaKind.Experience,
            ["פאדל", "טניס"], [Thrilled], AT, Any, Any, 200, 350, 3, 1, 3, 3, "ball"),
        I("סיור היסטורי מודרך בירושלים", "סיפורים שלא מכירים, במקומות שחשבנו שאנחנו מכירים.", IdeaKind.Experience,
            ["היסטוריה", "ירושלים", "טיולים בארץ"], [Thrilled, Moved], A, Any, Any, 250, 450, 3, 1, 3, 4, "book"),
        I("מנוי לקפה טרי לבית", "פולים טריים שמגיעים כל חודש.", IdeaKind.Product,
            ["קפה", "קפה ביתי"], [Pampered], A, Any, Any, 150, 300, 2, 1, 2, 3, "coffee"),
        I("ערכת גינון תבלינים לחלון", "גינה קטנה במטבח, ריח של בזיליקום.", IdeaKind.Product,
            ["גינון", "צמחי בית", "בישול"], [Moved], A, Any, Any, 120, 220, 2, 1, 2, 3, "plant"),
        I("כרטיסים להצגה + ארוחה לפני", "ערב תרבות מושלם, בלי לחשוב על כלום.", IdeaKind.Experience,
            ["תיאטרון", "מסעדות שכונתיות"], [Moved, Pampered], A, Any, Any, 350, 600, 3, 1, 2, 4, "ticket"),
        I("מסגרת תמונות דיגיטלית עם תמונות המשפחה", "תמונות הנכדים מתחלפות לבד, כל יום.", IdeaKind.Personal,
            ["נכדים", "זמן משפחה", "צילום"], [Moved], A, Family, Any, 300, 500, 3, 2, 3, 3, "frame"),
        I("יום כיף משפחתי מתוכנן", "כל המשפחה, יום אחד, מתוכנן מההתחלה עד הסוף.", IdeaKind.Bundle,
            ["זמן משפחה", "טיולים בארץ", "נכדים"], [Moved, Laugh], A, Close, Any, 300, 700, 4, 4, 3, 5, "heart"),
        I("מארז קפה ושוקולד", "תודה קטנה שעושה את הבוקר.", IdeaKind.Product,
            ["קפה", "שוקולד"], [Pampered], A, Any, Any, 80, 180, 1, 1, 1, 2, "coffee"),
        I("עציץ מעוצב לשולחן", "משהו ירוק שנשאר.", IdeaKind.Product,
            ["צמחי בית", "עיצוב הבית"], [Moved], A, Any, Any, 90, 180, 1, 1, 2, 2, "plant"),
        I("ספר עם הקדשה אישית", "ספר שבחרת במיוחד, עם כמה מילים ממך.", IdeaKind.Personal,
            ["ספרים", "קורסים ולמידה"], [Moved], AT, Any, Any, 60, 120, 2, 2, 2, 2, "book"),
        I("סניקרס במהדורה מיוחדת", "הזוג שכולם ישאלו עליו.", IdeaKind.Product,
            ["סניקרס", "אופנת רחוב"], [Thrilled], AT, Any, Any, 450, 800, 2, 1, 2, 3, "sneaker"),
        I("כרטיס מתנה למשחקים + אביזר גיימינג", "משחק חדש ובקר או אוזניות.", IdeaKind.Product,
            ["גיימינג", "מחשבים"], [Thrilled], T, Any, Any, 200, 400, 2, 1, 1, 1, "game"),
        I("סדנת יצירת תוכן ווידאו", "צילום, עריכה וסיפור, עם יוצר/ת תוכן אמיתי/ת.", IdeaKind.Experience,
            ["יצירת תוכן", "מצלמות"], [Thrilled], T, Any, Any, 250, 400, 3, 1, 4, 5, "camera"),
        I("ערכת לגו גדולה", "שעות של בנייה, ותוצאה שמציגים על המדף.", IdeaKind.Product,
            ["לגו", "בנייה"], [Thrilled], K, Any, Any, 150, 450, 2, 1, 1, 2, "lego"),
        I("ערכת מדע לניסויים בבית", "הרי געש, קריסטלים וטילים קטנים.", IdeaKind.Product,
            ["ערכות מדע", "חלל וחייזרים"], [Thrilled, Laugh], K, Any, Any, 120, 250, 3, 2, 3, 2, "rocket"),
        I("יום בפארק הדינוזאורים", "הרפתקה בין ענקים, עם כובע חוקר.", IdeaKind.Experience,
            ["דינוזאורים", "בעלי חיים"], [Thrilled, Adventure], K, Any, Any, 200, 400, 4, 2, 4, 4, "dino"),
        I("מופע קוסמות אישי בבית", "קוסם מגיע הביתה, והילד הוא העוזר על הבמה.", IdeaKind.Experience,
            ["קוסמים"], [Laugh, Surprised], K, Any, Any, 450, 800, 5, 2, 4, 7, "star"),
        I("אופניים או קורקינט חדש", "חופש על גלגלים.", IdeaKind.Product,
            ["אופניים וקורקינט"], [Thrilled], K, Any, Any, 300, 700, 3, 1, 1, 2, "ball"),
        I("ספר ילדים מותאם אישית", "סיפור שהילד הוא הגיבור שלו, עם השם שלו.", IdeaKind.Personal,
            ["ספרי ילדים"], [Moved], K, Any, Any, 120, 200, 3, 2, 4, 10, "book"),
        I("יום בפארק טרמפולינות", "קפיצות, צחוק ועייפות טובה.", IdeaKind.Experience,
            ["טרמפולינה", "מגרשי משחקים"], [Laugh], K, Any, Any, 90, 160, 3, 1, 2, 1, "star"),
        I("מארז לידה: בגדי תינוק + פינוק להורים", "משהו קטן לתינוק ומשהו טוב להורים העייפים.", IdeaKind.Product,
            [], [Moved], K, Any, ["birth", "brit"], 200, 350, 2, 1, 2, 2, "gift"),
        I("כרטיסים להצגת ילדים", "שעה של קסם על הבמה.", IdeaKind.Experience,
            ["סרטים מצוירים", "מוזיקה לילדים"], [Laugh], K, Any, Any, 120, 250, 3, 1, 2, 3, "ticket")
    ];

    public static IReadOnlyList<Partner> PartnerList() =>
    [
        P("פיקניקים מעוצבים · מרכז (דמו)", "מרכז"),
        P("סטודיו הדפסה · אונליין (דמו)", "אונליין"),
        P("ספא · השרון (דמו)", "שרון"),
        P("סטודיו צילום · תל אביב (דמו)", "תל אביב"),
        P("סטודיו קרמיקה · יפו (דמו)", "תל אביב"),
        P("מסעדת שף · תל אביב (דמו)", "תל אביב"),
        P("חנות פרחים · רמת גן (דמו)", "מרכז"),
        P("יקב בוטיק · יהודה (דמו)", "ירושלים"),
        P("שייט · נמל יפו (דמו)", "תל אביב"),
        P("טיולי שטח · הנגב (דמו)", "דרום"),
        P("צורפת · ירושלים (דמו)", "ירושלים"),
        P("חוויות לילדים · מרכז (דמו)", "מרכז")
    ];

    private static Partner P(string name, string region) =>
        Partner.Create(IdFor("partner", name), name, region, $"https://example.com/{Slug(name)}", verified: true);

    /// <summary>(idea title, partner name, list price, discount %). Offers with a discount are the business model.</summary>
    private static readonly (string Idea, string Partner, int ListPrice, int Discount)[] OfferRows =
    [
        ("פיקניק שקיעה אישי", "פיקניקים מעוצבים · מרכז (דמו)", 700, 15),
        ("יום הולדת בשלושה רגעים", "פיקניקים מעוצבים · מרכז (דמו)", 750, 10),
        ("אלבום מהטיולים שלכם", "סטודיו הדפסה · אונליין (דמו)", 360, 20),
        ("בוקר ספא זוגי + פרחים בחדר", "ספא · השרון (דמו)", 640, 0),
        ("סדנת צילום שקיעה עם צלם", "סטודיו צילום · תל אביב (דמו)", 530, 15),
        ("סדנת קרמיקה זוגית", "סטודיו קרמיקה · יפו (דמו)", 470, 10),
        ("ארוחת טעימות אצל שף", "מסעדת שף · תל אביב (דמו)", 750, 15),
        ("זר עונתי + שוקולד בוטיק", "חנות פרחים · רמת גן (דמו)", 270, 10),
        ("הדפסת צילום אהוב על קנבס", "סטודיו הדפסה · אונליין (דמו)", 260, 20),
        ("סיור טעימות ביקב", "יקב בוטיק · יהודה (דמו)", 420, 15),
        ("עגילים בעיצוב אישי", "צורפת · ירושלים (דמו)", 430, 10),
        ("שייט שקיעה ביפו", "שייט · נמל יפו (דמו)", 700, 15),
        ("טיול ג׳יפים במדבר", "טיולי שטח · הנגב (דמו)", 690, 10),
        ("עיסוי שוודי", "ספא · השרון (דמו)", 380, 15),
        ("יום בפארק הדינוזאורים", "חוויות לילדים · מרכז (דמו)", 320, 10),
        ("מופע קוסמות אישי בבית", "חוויות לילדים · מרכז (דמו)", 650, 10)
    ];

    public static IReadOnlyList<Offer> Offers(IReadOnlyList<CatalogIdea> ideas, IReadOnlyList<Partner> partners) =>
        OfferRows
            .Where(r => ideas.Any(i => i.Title == r.Idea))
            .Select(r =>
            {
                var idea = ideas.First(i => i.Title == r.Idea);
                var partner = partners.First(p => p.Name == r.Partner);
                return Offer.Create(IdFor("offer", r.Idea + r.Partner), idea.Id, partner.Id, r.ListPrice, r.Discount,
                    $"{partner.Website}/book/{Slug(r.Idea)}", "GIFT" + r.Discount.ToString("00"), PriceSource.ProviderListed, Verified);
            })
            .ToList();

    private static string Slug(string s) => Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(s)))[..10].ToLowerInvariant();
}
