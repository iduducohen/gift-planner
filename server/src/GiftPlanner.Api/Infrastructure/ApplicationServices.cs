using GiftPlanner.Application.Common;
using GiftPlanner.Application.Identity;
using GiftPlanner.Application.Orders;
using GiftPlanner.Application.Plans;
using GiftPlanner.Application.Recipients;
using GiftPlanner.Application.Recommendations;
using GiftPlanner.Application.Recommendations.Clarification;
using GiftPlanner.Application.Recommendations.Composition;
using GiftPlanner.Application.Recommendations.Refine;
using GiftPlanner.Application.Recommendations.Scoring;
using GiftPlanner.Application.Saved;
using GiftPlanner.Application.Sessions;
using GiftPlanner.Application.Taxonomy;
using GiftPlanner.Infrastructure.External;
using Microsoft.Extensions.Options;

namespace GiftPlanner.Api.Infrastructure;

/// <summary>
/// Composition root for the Application layer. The Application project has no DI dependency (it only
/// declares ports and use cases); wiring lives here, at the outermost layer.
/// Open/closed in practice: a new score component, filter, refine preset or offer provider is one line here.
/// </summary>
public static class ApplicationServices
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration config)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpCurrentUser>();

        services.AddSingleton<ITaxonomyService, TaxonomyService>();

        // Recommendation engine — hard filters, then weighted score components.
        services.AddSingleton<IHardFilter, AudienceFilter>();
        services.AddSingleton<IHardFilter, BudgetFilter>();
        services.AddSingleton<IHardFilter, AvoidFilter>();
        services.AddSingleton<IHardFilter, HistoryFilter>();
        services.AddSingleton<IHardFilter, LeadTimeFilter>();
        services.AddSingleton<IHardFilter, KindFilter>();
        services.AddSingleton<IHardFilter, RelationshipFilter>();
        services.AddSingleton<IHardFilter, OccasionFilter>();

        services.AddSingleton<IScoreComponent, InterestScore>();
        services.AddSingleton<IScoreComponent, EmotionScore>();
        services.AddSingleton<IScoreComponent, BudgetScore>();
        services.AddSingleton<IScoreComponent, OccasionScore>();
        services.AddSingleton<IScoreComponent, RelationshipScore>();
        services.AddSingleton<IScoreComponent, NoveltyScore>();
        services.AddSingleton<IScoreComponent, AvailabilityScore>();

        services.AddSingleton<IRecommendationScorer, RecommendationScorer>();
        services.AddSingleton<IDiversityReranker, DiversityReranker>();

        services.AddSingleton<IRefineStrategy, CheaperStrategy>();
        services.AddSingleton<IRefineStrategy, MoreExcitingStrategy>();
        services.AddSingleton<IRefineStrategy, MoreSurprisingStrategy>();
        services.AddSingleton<IRefineStrategy, ExperiencesOnlyStrategy>();
        services.AddSingleton<IRefineStrategy, ProductsOnlyStrategy>();
        services.AddSingleton<IRefineStrategy, MorePersonalStrategy>();
        services.AddSingleton<IRefineStrategyResolver, RefineStrategyResolver>();
        services.AddSingleton<IRefineTextInterpreter, KeywordRefineInterpreter>();

        services.AddSingleton<IClarificationPolicy, RuleBasedClarificationPolicy>();
        services.AddSingleton<IPlanGenerator, TemplatePlanGenerator>();
        services.AddScoped<IOfferResolver, OfferResolver>();

        // Composer chain (decorators): Caching → Fallback(LLM, Template).
        services.AddSingleton<TemplateIdeaComposer>();
        services.AddScoped<LlmIdeaComposer>();
        services.AddScoped<IIdeaComposer>(sp =>
        {
            var llmOptions = sp.GetRequiredService<IOptions<LlmOptions>>().Value;
            var llm = sp.GetRequiredService<ILlmClient>();
            var fallback = new FallbackIdeaComposer(
                sp.GetRequiredService<LlmIdeaComposer>(),
                sp.GetRequiredService<TemplateIdeaComposer>(),
                llm,
                TimeSpan.FromSeconds(llmOptions.TimeoutSeconds));
            var cacheHours = config.GetValue("Llm:CacheHours", 24);
            return new CachingIdeaComposer(fallback, sp.GetRequiredService<ICacheService>(), TimeSpan.FromHours(cacheHours));
        });

        // Use cases (one service per area — SRP).
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IGiftSessionService, GiftSessionService>();
        services.AddScoped<IIdeaService, IdeaService>();
        services.AddScoped<IRecipientService, RecipientService>();
        services.AddScoped<ISavedService, SavedService>();
        services.AddScoped<IPlanService, PlanService>();
        services.AddScoped<IPurchaseService, PurchaseService>();

        return services;
    }
}
