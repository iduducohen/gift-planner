# רגע · AI Gift & Surprise Planner

> אל תחפש מתנה. צור רגע.

אפליקציה היברידית (Ionic + Angular) עם שרת ‎.NET 10‎ שנבנה לפי SOLID. היא ממליצה על מתנות וחוויות לשוק הישראלי, בונה תוכנית ליום עצמו ומעבירה ביקוש לעסקים שותפים שנותנים הנחה.

```
gift-planner/
├── server/                     .NET 10 · modular monolith · Clean Architecture
│   ├── src/GiftPlanner.Domain          ישויות וחוקים עסקיים (ללא תלויות)
│   ├── src/GiftPlanner.Application     Use cases + ports (ממשקים). ללא NuGet
│   ├── src/GiftPlanner.Infrastructure  EF Core/PostgreSQL, Redis, Anthropic, JWT, OIDC
│   ├── src/GiftPlanner.Api             Minimal API, composition root, rate limits, OTel
│   └── tests/GiftPlanner.Application.Tests
├── client/                     Ionic 9 + Angular 22 (standalone, signals, zoneless) + Capacitor 8
└── docker-compose.yml          postgres · redis · api · client
```

## הרצה מהירה (Docker)

```bash
export JWT_SIGNING_KEY=$(openssl rand -hex 32)
export ANTHROPIC_API_KEY=...        # לא חובה. בלי מפתח, הניסוח מגיע מתבניות
docker compose up --build
# http://localhost:8100
```

בסביבת פיתוח השרת יוצר סכמה ומזריע קטלוג הדגמה: כ־50 רעיונות, 12 עסקים שותפים ו־15 הצעות עם הנחה. כל הקישורים מפנים ל־example.com.

## פיתוח מקומי

**שרת**

```bash
cd server
dotnet user-secrets --project src/GiftPlanner.Api set "Jwt:SigningKey" "$(openssl rand -hex 32)"
dotnet user-secrets --project src/GiftPlanner.Api set "Llm:ApiKey" "sk-ant-..."   # לא חובה
dotnet run --project src/GiftPlanner.Api --urls http://localhost:5080
dotnet test
```

מיגרציות (מומלץ לפני פרודקשן; בלעדיהן בפיתוח רץ `EnsureCreated`):

```bash
dotnet tool install -g dotnet-ef
dotnet ef migrations add Initial -p src/GiftPlanner.Infrastructure -s src/GiftPlanner.Api -o Persistence/Migrations
```

**קליינט**

```bash
cd client
npm ci
npm start            # http://localhost:4200, עם proxy ל־/api → :5080
npm run mock         # שרת API מדומה ב־Node (בלי .NET/DB). מתאים לעבודת UI
npx cap add ios && npx cap add android && npx ng build && npx cap sync
```

## קונפיגורציה

| מפתח | תיאור |
|---|---|
| `ConnectionStrings:Postgres` | חובה |
| `ConnectionStrings:Redis` | לא חובה. בלעדיו השרת משתמש בקאש בזיכרון |
| `Jwt:SigningKey` | חובה, לפחות 32 בתים. **רק** ממשתנה סביבה או user-secrets, לעולם לא ב־git |
| `Llm:ApiKey`, `Llm:FastModel`, `Llm:SmartModel` | מזהי המודלים הם קונפיגורציה. בלי מפתח עוברים לתבניות |
| `Auth:GoogleClientId`, `Auth:AppleClientId` | אימות ID tokens בצד השרת |
| `Auth:EnableDevProvider` | כניסת הדגמה. **לפיתוח בלבד** |
| `client/public/config.js` | Client IDs ציבוריים של Google/Apple בזמן ריצה |

## איך SOLID מתבטא בקוד

- **S**: כל use case הוא שירות נפרד (`GiftSessionService`, `IdeaService`, `PlanService`, `PurchaseService`, …). ה־endpoints רק מתרגמים HTTP.
- **O**: מנוע ההמלצות מורכב מרכיבים:
  - `IHardFilter` (8), `IScoreComponent` (7), `IRefineStrategy` (6) ו־`IOfferProvider`.
  - כלל או ספק חדש = מחלקה חדשה ושורה אחת ב־`ApplicationServices`.
- **L**: כל `IIdeaComposer` ניתן להחלפה:
  - `Template` ו־`Llm` הם מימושים;
  - `Fallback` ו־`Caching` הם decorators.
- **I**: ממשקי repository קטנים לכל aggregate. לדוגמה, `IPurchaseIntentRepository` מכיל רק `Add`.
- **D**: שכבת ה־Application מגדירה ports (`ILlmClient`, `ICacheService`, `IClock`, `ICurrentUser`, …). Infrastructure מממשת אותם. ל־Application אין אף חבילת NuGet.

**עקרונות נוספים**

- ה־LLM לא קובע מחירים ולא כותב קישורים, וה־parser דוחה פלט כזה.
- חסות או הנחה לא משפיעות על הדירוג.
- כל שאילתה מסוננת לפי `UserId` בתוך ה־repository, כהגנה מפני IDOR.
- כניסה אנונימית קודם. בהתחברות הנתונים ממוזגים לחשבון.
- ייצוא ומחיקה מלאה של הנתונים דרך `/api/v1/me`.

## API (עיקרי)

`POST /api/v1/auth/anonymous` · `POST /auth/sign-in` · `GET /taxonomy/{relationships|occasions|age-ranges|interests}` · `POST /gift-sessions` · `POST /gift-sessions/{id}/clarifications` · `POST /gift-sessions/{id}/idea-sets` · `GET /idea-sets/{id}` · `GET /idea-sets/{id}/ideas?cursor&limit&filter` · `GET /ideas/{id}` · `PUT|DELETE /ideas/{id}/saved` · `POST /ideas/{id}/plan` · `POST /ideas/{id}/purchase-intents` · `/recipients…` · `GET /saved` · `/plans…` · `GET|DELETE /me…` · `GET /health`

ב־Development יש OpenAPI בכתובת `/openapi/v1.json`.

## קליינט: מה ממומש

- **שאלון בשלושה שלבים**:
  - בוחרים עם חיפוש: למי המתנה, איזה אירוע וטווח גיל;
  - בחירה מרובה של תחומי עניין מותאמים. הרשימה המלאה (150 פריטים ומעלה) מוצגת ב־CDK virtual scroll;
  - "מה לא להציע", תקציב ורגש.
- **שאלת הבהרה**: אחת לכל היותר.
- **תוצאות**:
  - 5 הרעיונות המובילים בקרוסלה;
  - שאר הרעיונות בגריד וירטואלי (`@tanstack/angular-virtual`) עם טעינה עצלה בגלילה ו־skeletons;
  - סינון, ושינוי כיוון בלחיצה על preset או בטקסט חופשי.
- **פרטי רעיון**: עמוד בנייד, מגירה בדסקטופ. כולל שמירה, שיתוף, "הפוך לתוכנית" ורכישה עם קוד הנחה וקישור מעקב, ואנימציית **קונפטי ובלונים** בבחירה הסופית (מכבדת `prefers-reduced-motion`, עם haptics).
- **מסכים נוספים**: תוכנית עם ציר זמן ומשימות, האנשים שלי, פרופיל אדם, שמורים (רעיונות ותוכניות) וכניסה.
- **תשתית**: RTL מלא, עברית מותאמת מגדר לפי הקשר, רספונסיבי (טאבים בנייד, ניווט עליון בדסקטופ).

## מצב האימות

| רכיב | נבדק |
|---|---|
| Domain + Application | קומפילציה נקייה, ו־26 בדיקות עוברות (מנוע ההמלצות, פילטרים, refine, diversity, clarification, parser, תאריכים ובדיקת קטלוג) |
| Infrastructure + Api | **לא קומפלו בסביבת הבנייה** כי הגישה ל־NuGet הייתה חסומה שם. ה־CI ב־GitHub Actions מקמפל ומריץ בדיקות |
| Client | `ng build` נקי. הזרימה המלאה נבדקה בדפדפן מול שרת ה־mock, בנייד (390px) ובדסקטופ (1440px) |
