using GiftPlanner.Domain.Common;

namespace GiftPlanner.Application.Taxonomy;

public enum Gender
{
    Female = 1,
    Male = 2,
    Neutral = 3
}

public sealed record Relationship(string Key, string Label, string Group, Gender Gender, Audience? FixedAudience = null);

public sealed record AgeRange(string Key, string Label, string Group, Audience Audience);

public sealed record OccasionType(string Key, string Label, string Group, string? Note = null);

public sealed record InterestCategory(string Id, string Title, IReadOnlyList<Audience> Audiences, IReadOnlyList<string> Items);

/// <summary>
/// The fixed vocabularies behind every list in the UI. Keys are stable identifiers stored in the database;
/// labels are Hebrew display text. Interests use the label as the key so the curated catalog can be tagged by hand.
/// </summary>
public static class TaxonomyData
{
    private static readonly Audience[] Adult = [Audience.Adult];
    private static readonly Audience[] AdultTeen = [Audience.Adult, Audience.Teen];
    private static readonly Audience[] Kid = [Audience.Kid];

    public static readonly IReadOnlyList<Relationship> Relationships =
    [
        new("partner_f", "בת זוג / אשתי", "זוגיות", Gender.Female, Audience.Adult),
        new("partner_m", "בן זוג / בעלי", "זוגיות", Gender.Male, Audience.Adult),
        new("fiancee", "ארוסה", "זוגיות", Gender.Female, Audience.Adult),
        new("fiance", "ארוס", "זוגיות", Gender.Male, Audience.Adult),
        new("new_date", "דייט חדש/ה", "זוגיות", Gender.Neutral, Audience.Adult),
        new("mom", "אמא", "משפחה קרובה", Gender.Female, Audience.Adult),
        new("dad", "אבא", "משפחה קרובה", Gender.Male, Audience.Adult),
        new("daughter", "בת", "משפחה קרובה", Gender.Female),
        new("son", "בן", "משפחה קרובה", Gender.Male),
        new("sister", "אחות", "משפחה קרובה", Gender.Female),
        new("brother", "אח", "משפחה קרובה", Gender.Male),
        new("grandma", "סבתא", "משפחה קרובה", Gender.Female, Audience.Adult),
        new("grandpa", "סבא", "משפחה קרובה", Gender.Male, Audience.Adult),
        new("granddaughter", "נכדה", "משפחה קרובה", Gender.Female),
        new("grandson", "נכד", "משפחה קרובה", Gender.Male),
        new("mother_in_law", "חמותי", "משפחה מורחבת", Gender.Female, Audience.Adult),
        new("father_in_law", "חמי", "משפחה מורחבת", Gender.Male, Audience.Adult),
        new("sister_in_law", "גיסה", "משפחה מורחבת", Gender.Female, Audience.Adult),
        new("brother_in_law", "גיס", "משפחה מורחבת", Gender.Male, Audience.Adult),
        new("aunt", "דודה", "משפחה מורחבת", Gender.Female, Audience.Adult),
        new("uncle", "דוד", "משפחה מורחבת", Gender.Male, Audience.Adult),
        new("niece", "אחיינית", "משפחה מורחבת", Gender.Female),
        new("nephew", "אחיין", "משפחה מורחבת", Gender.Male),
        new("cousin", "בת דודה / בן דוד", "משפחה מורחבת", Gender.Neutral),
        new("friend_f", "חברה טובה", "חברים", Gender.Female),
        new("friend_m", "חבר טוב", "חברים", Gender.Male),
        new("couple_friends", "זוג חברים", "חברים", Gender.Neutral, Audience.Adult),
        new("childhood_friend", "חבר/ה מהילדות", "חברים", Gender.Neutral),
        new("roommate", "שותף/ה לדירה", "חברים", Gender.Neutral),
        new("friends_kid", "ילד/ה של חברים", "ילדים של אחרים", Gender.Neutral, Audience.Kid),
        new("kids_classmate", "חבר/ה מהכיתה של הילד", "ילדים של אחרים", Gender.Neutral, Audience.Kid),
        new("newborn", "תינוק/ת שנולד/ה", "ילדים של אחרים", Gender.Neutral, Audience.Kid),
        new("colleague", "עמית/ה לעבודה", "עבודה", Gender.Neutral, Audience.Adult),
        new("manager", "מנהל/ת", "עבודה", Gender.Neutral, Audience.Adult),
        new("employee", "עובד/ת", "עבודה", Gender.Neutral, Audience.Adult),
        new("team", "צוות שלם", "עבודה", Gender.Neutral, Audience.Adult),
        new("client", "לקוח/ה", "עבודה", Gender.Neutral, Audience.Adult),
        new("teacher", "מורה / גננת", "עוד", Gender.Female, Audience.Adult),
        new("babysitter", "בייביסיטר", "עוד", Gender.Neutral),
        new("neighbors", "שכנים", "עוד", Gender.Neutral, Audience.Adult),
        new("hosts", "מארחים (מתנה לבית)", "עוד", Gender.Neutral, Audience.Adult),
        new("group_gift", "מתנה משותפת מקבוצה", "עוד", Gender.Neutral)
    ];

    public static readonly IReadOnlyList<string> CommonRelationships = ["partner_f", "partner_m", "mom", "dad", "friend_f", "friend_m"];

    public static readonly IReadOnlyList<AgeRange> AgeRanges =
    [
        new("0-1", "תינוק/ת (עד שנה)", "ילדים", Audience.Kid),
        new("1-3", "פעוט/ה (1–3)", "ילדים", Audience.Kid),
        new("3-6", "גן (3–6)", "ילדים", Audience.Kid),
        new("6-9", "6–9", "ילדים", Audience.Kid),
        new("9-12", "9–12", "ילדים", Audience.Kid),
        new("13-15", "13–15", "נוער", Audience.Teen),
        new("16-18", "16–18", "נוער", Audience.Teen),
        new("18-21", "18–21 (צבא)", "צעירים", Audience.Adult),
        new("22-25", "22–25", "צעירים", Audience.Adult),
        new("26-30", "26–30", "צעירים", Audience.Adult),
        new("31-35", "31–35", "מבוגרים", Audience.Adult),
        new("36-40", "36–40", "מבוגרים", Audience.Adult),
        new("41-45", "41–45", "מבוגרים", Audience.Adult),
        new("46-50", "46–50", "מבוגרים", Audience.Adult),
        new("51-55", "51–55", "מבוגרים", Audience.Adult),
        new("56-60", "56–60", "מבוגרים", Audience.Adult),
        new("61-65", "61–65", "גיל הזהב", Audience.Adult),
        new("66-70", "66–70", "גיל הזהב", Audience.Adult),
        new("71-80", "71–80", "גיל הזהב", Audience.Adult),
        new("80+", "80+", "גיל הזהב", Audience.Adult)
    ];

    public static readonly IReadOnlyList<OccasionType> Occasions =
    [
        new("birthday", "יום הולדת", "ימי הולדת וזוגיות"),
        new("milestone_birthday", "יום הולדת עגול", "ימי הולדת וזוגיות", "נשאל איזה גיל: 30, 40, 50…"),
        new("anniversary", "יום נישואין", "ימי הולדת וזוגיות"),
        new("dating_anniversary", "יום השנה לזוגיות", "ימי הולדת וזוגיות", "לזוגות שלא נשואים"),
        new("proposal", "הצעת נישואין", "ימי הולדת וזוגיות"),
        new("wedding", "חתונה", "ימי הולדת וזוגיות"),
        new("bachelor", "מסיבת רווקות / רווקים", "ימי הולדת וזוגיות"),
        new("birth", "לידה", "משפחה ואבני דרך"),
        new("brit", "ברית / בריתה", "משפחה ואבני דרך"),
        new("bar_mitzvah", "בר מצווה", "משפחה ואבני דרך"),
        new("bat_mitzvah", "בת מצווה", "משפחה ואבני דרך"),
        new("housewarming", "חנוכת בית", "משפחה ואבני דרך"),
        new("moving", "מעבר דירה", "משפחה ואבני דרך"),
        new("school_end", "סיום גן / בית ספר", "משפחה ואבני דרך"),
        new("rosh_hashana", "ראש השנה", "חגים וימים מיוחדים"),
        new("hanukkah", "חנוכה", "חגים וימים מיוחדים"),
        new("purim", "פורים", "חגים וימים מיוחדים", "גם משלוח מנות"),
        new("passover", "פסח", "חגים וימים מיוחדים"),
        new("tu_beav", "ט״ו באב", "חגים וימים מיוחדים"),
        new("valentines", "יום האהבה", "חגים וימים מיוחדים"),
        new("family_day", "יום המשפחה", "חגים וימים מיוחדים"),
        new("womens_day", "יום האישה", "חגים וימים מיוחדים"),
        new("graduation", "סיום תואר / לימודים", "הישגים ושינויים"),
        new("enlistment", "גיוס לצבא", "הישגים ושינויים"),
        new("release", "שחרור מהצבא", "הישגים ושינויים"),
        new("new_job", "עבודה חדשה / קידום", "הישגים ושינויים"),
        new("retirement", "פרישה", "הישגים ושינויים"),
        new("relocation", "רילוקיישן / טיול גדול", "הישגים ושינויים"),
        new("recovery", "החלמה", "הישגים ושינויים"),
        new("thanks", "תודה", "מחוות"),
        new("apology", "סליחה / פיוס", "מחוות"),
        new("encouragement", "עידוד בתקופה קשה", "מחוות"),
        new("farewell", "פרידה (עזיבת עבודה)", "מחוות"),
        new("just_because", "סתם כי בא לי", "מחוות")
    ];

    public static readonly IReadOnlyList<string> CommonOccasions = ["birthday", "anniversary", "valentines", "birth", "housewarming", "just_because"];

    public static readonly IReadOnlyList<InterestCategory> Interests =
    [
        new("food", "אוכל ושתייה", AdultTeen, ["מסעדות שף", "מסעדות שכונתיות", "בישול", "אפייה", "על האש", "יין", "וויסקי", "קוקטיילים", "בירה", "קפה", "תה", "שוקולד", "קינוחים", "אוכל בריא", "טבעוני / צמחוני", "אוכל אסייתי", "סושי", "אוכל איטלקי", "ארוחות בוקר", "גבינות", "שווקים ואוכל רחוב"]),
        new("travel", "טיולים וחופשות", AdultTeen, ["טיולים בארץ", "טיולים בחו״ל", "סופ״ש בצימר", "מלונות בוטיק", "קמפינג", "גלמפינג", "ים וחופים", "מדבר", "צפון וגליל", "ירושלים", "שטח וג׳יפים", "מעיינות ונחלים", "טרקים", "טבע וציפורים", "שקיעות"]),
        new("sport", "ספורט ותנועה", AdultTeen, ["ריצה", "הליכות", "יוגה", "פילאטיס", "חדר כושר", "אופניים", "רכיבת שטח", "שחייה", "טניס", "פאדל", "גולף", "גלישה", "SUP", "צלילה", "סקי", "ריקוד", "טיפוס", "איגרוף", "אומנויות לחימה", "צפייה בכדורגל", "צפייה בכדורסל", "סקייטבורד"]),
        new("pamper", "פינוק ויופי", AdultTeen, ["ספא", "עיסויים", "טיפוח פנים", "איפור", "בשמים", "ציפורניים", "שיער", "פרחים", "נרות וריחות", "פיג׳מות ונוחות"]),
        new("fashion", "אופנה ואקססוריז", AdultTeen, ["בגדים", "אופנת רחוב", "סניקרס", "נעליים", "תיקים", "תכשיטים", "שעונים", "משקפי שמש", "כובעים", "וינטג׳"]),
        new("create", "יצירה ואמנות", AdultTeen, ["צילום", "ציור", "רישום", "קרמיקה", "תכשיטנות", "סריגה", "תפירה", "רקמה", "נגרות", "DIY", "קליגרפיה", "כתיבה", "עיצוב גרפי", "יצירת תוכן"]),
        new("home", "בית וגינה", Adult, ["עיצוב הבית", "צמחי בית", "גינון", "אירוח", "כלי בית מיוחדים", "קפה ביתי", "סדר וארגון"]),
        new("culture", "מוזיקה ותרבות", AdultTeen, ["מוזיקה ישראלית", "רוק", "ג׳אז", "מוזיקה קלאסית", "היפ הופ", "K-pop", "הופעות", "פסטיבלים", "תיאטרון", "מחול", "סטנדאפ", "קולנוע", "סדרות", "אנימה", "ספרים", "קומיקס", "שירה", "מוזיאונים", "היסטוריה", "פודקאסטים"]),
        new("tech", "טכנולוגיה", AdultTeen, ["גאדג׳טים", "סמארטפונים", "אוזניות וסאונד", "בית חכם", "מחשבים", "רחפנים", "מצלמות", "גיימינג", "VR", "מדע וחלל"]),
        new("games", "משחקים וחוויות", AdultTeen, ["משחקי קופסה", "פאזלים", "חדרי בריחה", "פוקר", "טריוויה", "קריוקי", "לגו למבוגרים", "מסיבות"]),
        new("people", "משפחה, חברים וחיות", Adult, ["זמן משפחה", "ילדים", "נכדים", "ערבי חברים", "כלבים", "חתולים", "סוסים"]),
        new("growth", "התפתחות ורוחניות", AdultTeen, ["מדיטציה", "מיינדפולנס", "יהדות ומסורת", "אסטרולוגיה", "פסיכולוגיה", "קורסים ולמידה", "שפות", "התנדבות", "יזמות", "השקעות"]),
        new("ride", "רכב ותחבורה", AdultTeen, ["רכבים", "אופנועים", "טרקטורונים", "אופניים חשמליים", "תעופה"]),
        new("kplay", "משחק ודמיון", Kid, ["לגו", "דינוזאורים", "יוניקורנים", "סופר-גיבורים", "בובות", "מכוניות ורכבות", "חלל וחייזרים", "נסיכות", "פיראטים", "משחקי תפקידים", "בובות פרווה"]),
        new("kcreate", "יצירה ומדע", Kid, ["צביעה ויצירה", "פלסטלינה", "חרוזים", "מדבקות", "ערכות מדע", "בנייה", "קוסמים", "מוזיקה לילדים"]),
        new("kmove", "תנועה ובחוץ", Kid, ["כדורגל", "אופניים וקורקינט", "טרמפולינה", "שחייה לילדים", "ג׳ודו / קראטה", "התעמלות", "בלט", "מגרשי משחקים", "בעלי חיים"]),
        new("kread", "סיפורים ומסכים", Kid, ["ספרי ילדים", "קומיקס לילדים", "סרטים מצוירים", "משחקי מחשב לילדים", "פאזלים לילדים"])
    ];

    /// <summary>Hand-picked "recommended for" lists. Falls back to the audience list when a relationship has none.</summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> RecommendedByRelationship =
        new Dictionary<string, IReadOnlyList<string>>
        {
            ["partner_f"] = ["מסעדות שף", "ספא", "סופ״ש בצימר", "תכשיטים", "פרחים", "צילום", "יין", "שקיעות", "הופעות", "בשמים", "קרמיקה", "יוגה"],
            ["partner_m"] = ["מסעדות שף", "וויסקי", "סופ״ש בצימר", "שעונים", "גאדג׳טים", "צפייה בכדורגל", "על האש", "שטח וג׳יפים", "הופעות", "אוזניות וסאונד", "קפה", "חדרי בריחה"],
            ["mom"] = ["גינון", "קרמיקה", "תיאטרון", "זמן משפחה", "נכדים", "ספא", "פרחים", "ספרים", "טיולים בארץ", "בישול", "מוזיקה ישראלית", "צמחי בית"],
            ["dad"] = ["היסטוריה", "וויסקי", "גינון", "מוזיקה ישראלית", "צפייה בכדורגל", "נכדים", "טיולים בארץ", "על האש", "ספרים", "קפה", "רכבים", "הליכות"],
            ["grandma"] = ["זמן משפחה", "נכדים", "בישול", "מוזיקה ישראלית", "צמחי בית", "ספרים", "תיאטרון", "פרחים"],
            ["grandpa"] = ["זמן משפחה", "נכדים", "היסטוריה", "מוזיקה ישראלית", "ספרים", "גינון", "הליכות", "קפה"],
            ["colleague"] = ["קפה", "ספרים", "צמחי בית", "שוקולד", "פודקאסטים", "יין", "מסעדות שכונתיות", "סדר וארגון", "גאדג׳טים", "יוגה", "מדיטציה", "נרות וריחות"],
            ["friend_f"] = ["מסעדות שכונתיות", "יין", "ספא", "הופעות", "קרמיקה", "ספרים", "יוגה", "נרות וריחות", "טיולים בארץ", "קוקטיילים", "סדרות", "צמחי בית"],
            ["friend_m"] = ["וויסקי", "בירה", "צפייה בכדורגל", "משחקי קופסה", "חדרי בריחה", "גאדג׳טים", "על האש", "הופעות", "סטנדאפ", "קפה", "שטח וג׳יפים", "פאדל"]
        };

    public static readonly IReadOnlyDictionary<Audience, IReadOnlyList<string>> RecommendedByAudience =
        new Dictionary<Audience, IReadOnlyList<string>>
        {
            [Audience.Kid] = ["לגו", "דינוזאורים", "ערכות מדע", "סופר-גיבורים", "חלל וחייזרים", "אופניים וקורקינט", "צביעה ויצירה", "קוסמים", "ספרי ילדים", "כדורגל", "בעלי חיים", "טרמפולינה"],
            [Audience.Teen] = ["גיימינג", "אופנת רחוב", "סניקרס", "אוזניות וסאונד", "יצירת תוכן", "אנימה", "K-pop", "סקייטבורד", "מצלמות", "מסיבות", "איפור", "פסטיבלים"],
            [Audience.Adult] = ["מסעדות שף", "טיולים בארץ", "ספא", "הופעות", "ספרים", "קפה", "יין", "צילום", "משחקי קופסה", "צמחי בית", "סדרות", "בישול"]
        };
}
