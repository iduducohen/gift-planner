using GiftPlanner.Application.Recommendations.Clarification;
using GiftPlanner.Application.Recommendations.Composition;
using GiftPlanner.Application.Recommendations.Refine;
using GiftPlanner.Application.Recommendations.Scoring;
using GiftPlanner.Domain.Catalog;
using GiftPlanner.Domain.Common;
using GiftPlanner.Domain.GiftSessions;
using GiftPlanner.Domain.Recipients;

namespace GiftPlanner.Application.Tests;

internal static class Fixtures
{
    public static readonly DateOnly Today = new(2026, 9, 28);

    public static GiftBrief Brief(int budget = 600, string[]? interests = null, string[]? avoid = null,
        Emotion[]? emotions = null, DateOnly? eventDate = null) =>
        GiftBrief.Create("partner_f", "birthday", eventDate, "35-44", interests ?? ["צילום", "שקיעות"], avoid, null, budget,
            emotions ?? [Emotion.Moved]);

    public static CatalogIdea Idea(string title, IdeaKind kind = IdeaKind.Experience, int min = 300, int max = 500,
        string[]? interests = null, Emotion[]? emotions = null, Audience[]? audiences = null, int surprise = 3,
        int lead = 1, string[]? includes = null) =>
        CatalogIdea.Create(Guid.NewGuid(), title, "desc", kind, interests ?? ["צילום"], emotions ?? [Emotion.Moved],
            [], audiences ?? [Audience.Adult], [], min, max, surprise, 2, 3, lead, "gift", null, includes ?? [], Today);

    public static ScoringContext Ctx(GiftBrief? brief = null, string[]? history = null) =>
        ScoringContext.Create(brief ?? Brief(), Audience.Adult, Today, history);

    public static RecommendationScorer Scorer() => new(
        [new AudienceFilter(), new BudgetFilter(), new AvoidFilter(), new HistoryFilter(), new LeadTimeFilter(), new KindFilter(), new RelationshipFilter(), new OccasionFilter()],
        [new InterestScore(), new EmotionScore(), new BudgetScore(), new OccasionScore(), new RelationshipScore(), new NoveltyScore(), new AvailabilityScore()]);
}

public class FilterTests
{
    [Fact]
    public void Budget_filter_allows_up_to_15_percent_over()
    {
        var ctx = Fixtures.Ctx(Fixtures.Brief(budget: 400));
        Assert.True(new BudgetFilter().Allows(Fixtures.Idea("a", min: 460), ctx));
        Assert.False(new BudgetFilter().Allows(Fixtures.Idea("b", min: 461), ctx));
    }

    [Fact]
    public void Avoid_filter_matches_title_interests_and_includes()
    {
        var ctx = Fixtures.Ctx(Fixtures.Brief(avoid: ["בושם"]));
        var f = new AvoidFilter();
        Assert.False(f.Allows(Fixtures.Idea("בושם יוקרתי"), ctx));
        Assert.False(f.Allows(Fixtures.Idea("מארז", includes: ["בושם קטן"]), ctx));
        Assert.True(f.Allows(Fixtures.Idea("סדנת צילום"), ctx));
    }

    [Fact]
    public void History_filter_blocks_previous_gifts()
    {
        var ctx = Fixtures.Ctx(history: ["סדנת קרמיקה"]);
        Assert.False(new HistoryFilter().Allows(Fixtures.Idea("סדנת קרמיקה"), ctx));
    }

    [Fact]
    public void Lead_time_filter_blocks_ideas_that_cannot_arrive_in_time()
    {
        var ctx = Fixtures.Ctx(Fixtures.Brief(eventDate: Fixtures.Today.AddDays(2)));
        Assert.False(new LeadTimeFilter().Allows(Fixtures.Idea("album", lead: 7), ctx));
        Assert.True(new LeadTimeFilter().Allows(Fixtures.Idea("flowers", lead: 1), ctx));
    }

    [Fact]
    public void Audience_filter_keeps_kid_ideas_away_from_adults()
    {
        Assert.False(new AudienceFilter().Allows(Fixtures.Idea("lego", audiences: [Audience.Kid]), Fixtures.Ctx()));
    }
}

public class ScoringTests
{
    [Fact]
    public void Matching_interests_rank_higher()
    {
        var match = Fixtures.Idea("match", interests: ["צילום", "שקיעות"]);
        var miss = Fixtures.Idea("miss", interests: ["גיימינג"]);
        var ranked = Fixtures.Scorer().Rank([miss, match], Fixtures.Ctx());
        Assert.Equal("match", ranked[0].Idea.Title);
    }

    [Fact]
    public void Scores_are_bounded_and_explainable()
    {
        var ranked = Fixtures.Scorer().Rank([Fixtures.Idea("x")], Fixtures.Ctx());
        Assert.InRange(ranked[0].Total, 0, 1.5);
        Assert.Contains(ScoreNames.Interests, ranked[0].Breakdown.Keys);
    }

    [Fact]
    public void Filtered_ideas_never_appear()
    {
        var ranked = Fixtures.Scorer().Rank([Fixtures.Idea("too expensive", min: 5000, max: 6000)], Fixtures.Ctx());
        Assert.Empty(ranked);
    }
}

public class DiversityTests
{
    private static ScoredIdea S(IdeaKind kind, double total, int surprise = 3) =>
        new(Fixtures.Idea($"{kind}-{total}", kind, surprise: surprise), total, new Dictionary<string, double>());

    [Fact]
    public void Top_has_at_most_two_of_a_kind_and_one_surprise()
    {
        var ranked = new[]
        {
            S(IdeaKind.Experience, .9), S(IdeaKind.Experience, .89), S(IdeaKind.Experience, .88),
            S(IdeaKind.Product, .8), S(IdeaKind.Product, .79), S(IdeaKind.Personal, .7), S(IdeaKind.Pamper, .5, surprise: 5)
        };
        var result = new DiversityReranker().Rerank(ranked, 5);
        var top = result.Take(5).ToList();

        Assert.True(top.GroupBy(s => s.Idea.Kind).All(g => g.Count() <= DiversityReranker.MaxPerKind));
        Assert.Contains(top, s => s.Idea.Surprise >= 4);
        Assert.Equal(ranked.Length, result.Count); // nothing is lost, only reordered
    }
}

public class RefineTests
{
    [Fact]
    public async Task Already_has_phrase_becomes_avoid_terms_including_singular()
    {
        var intent = await new KeywordRefineInterpreter().InterpretAsync("כבר יש לה הרבה בשמים", default);
        Assert.Contains("בשמים", intent.ExtraAvoid);
        Assert.Contains("בושם", intent.ExtraAvoid);
    }

    [Fact]
    public async Task Cheaper_keyword_maps_to_preset()
    {
        var intent = await new KeywordRefineInterpreter().InterpretAsync("משהו יותר זול", default);
        Assert.Equal("cheaper", intent.PresetKey);
    }

    [Fact]
    public void Cheaper_strategy_lowers_budget_cap()
    {
        var ctx = Fixtures.Ctx(Fixtures.Brief(budget: 600));
        Assert.True(new CheaperStrategy().Apply(ctx).BudgetCap < 600);
    }

    [Fact]
    public void Experiences_only_restricts_kinds()
    {
        var ctx = new ExperiencesOnlyStrategy().Apply(Fixtures.Ctx());
        Assert.False(new KindFilter().Allows(Fixtures.Idea("p", IdeaKind.Product), ctx));
        Assert.True(new KindFilter().Allows(Fixtures.Idea("e", IdeaKind.Experience), ctx));
    }
}

public class ClarificationTests
{
    [Fact]
    public void Asks_when_the_brief_is_thin()
    {
        var brief = Fixtures.Brief(interests: ["צילום"], emotions: []);
        Assert.NotNull(new RuleBasedClarificationPolicy().Decide(brief));
    }

    [Fact]
    public void Does_not_ask_when_the_brief_is_clear()
    {
        Assert.Null(new RuleBasedClarificationPolicy().Decide(Fixtures.Brief()));
    }

    [Fact]
    public void Answer_sets_the_emotion()
    {
        var policy = new RuleBasedClarificationPolicy();
        var brief = Fixtures.Brief(interests: ["צילום"], emotions: []);
        var q = policy.Decide(brief)!;
        Assert.Equal([Emotion.Pampered], policy.Apply(brief, q, "pamper").Emotions);
    }
}

public class LlmOutputParserTests
{
    private static readonly Guid A = Guid.NewGuid();

    [Fact]
    public void Parses_valid_output()
    {
        var text = $$"""{"ideas":[{"id":"{{A}}","why":"כי היא אוהבת שקיעות","emotionalValue":"רגע שלכם"}]}""";
        var result = LlmOutputParser.Parse(text, new HashSet<Guid> { A });
        Assert.Single(result);
    }

    [Theory]
    [InlineData("עולה ₪300")]
    [InlineData("ראו https://evil.example")]
    public void Rejects_prices_and_links(string why)
    {
        var text = $$"""{"ideas":[{"id":"{{A}}","why":"{{why}}"}]}""";
        Assert.Throws<FormatException>(() => LlmOutputParser.Parse(text, new HashSet<Guid> { A }));
    }

    [Fact]
    public void Rejects_unknown_ids()
    {
        var text = $$"""{"ideas":[{"id":"{{Guid.NewGuid()}}","why":"x"}]}""";
        Assert.Throws<FormatException>(() => LlmOutputParser.Parse(text, new HashSet<Guid> { A }));
    }
}

public class OccasionTests
{
    [Fact]
    public void Recurring_occasion_rolls_to_next_year()
    {
        var o = new Occasion("birthday", 3, 10, recurring: true, remindDaysBefore: 10);
        Assert.Equal(new DateOnly(2027, 3, 10), o.NextDate(Fixtures.Today));
    }

    [Fact]
    public void Feb_29_falls_back_to_feb_28_in_non_leap_years()
    {
        var o = new Occasion("birthday", 2, 29, recurring: true, remindDaysBefore: 10);
        Assert.Equal(new DateOnly(2027, 2, 28), o.NextDate(Fixtures.Today));
    }

    [Fact]
    public void Past_one_time_occasion_has_no_next_date()
    {
        var o = new Occasion("wedding", 1, 5, recurring: false, remindDaysBefore: 10);
        Assert.Null(o.NextDate(Fixtures.Today));
    }
}
