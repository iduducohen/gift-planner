using GiftPlanner.Application.Common;
using GiftPlanner.Infrastructure.External;
using GiftPlanner.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GiftPlanner.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<ClockOptions>(config.GetSection("Clock"));
        services.Configure<LlmOptions>(config.GetSection("Llm"));
        services.Configure<JwtOptions>(config.GetSection("Jwt"));
        services.Configure<OidcProviderOptions>(config.GetSection("Auth"));

        services.AddDbContext<AppDbContext>(o =>
            o.UseNpgsql(config.GetConnectionString("Postgres"), npg => npg.EnableRetryOnFailure(3)));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRecipientRepository, RecipientRepository>();
        services.AddScoped<IGiftSessionRepository, GiftSessionRepository>();
        services.AddScoped<IIdeaSetRepository, IdeaSetRepository>();
        services.AddScoped<ISavedIdeaRepository, SavedIdeaRepository>();
        services.AddScoped<IPlanRepository, PlanRepository>();
        services.AddScoped<IPurchaseIntentRepository, PurchaseIntentRepository>();
        services.AddScoped<ICatalogReader, CatalogReader>();
        services.AddScoped<IUserDataMaintenance, UserDataMaintenance>();

        // Supplier integrations: each new partner API is one more IOfferProvider registration.
        services.AddScoped<IOfferProvider, CuratedCatalogOfferProvider>();

        services.AddMemoryCache();
        var redis = config.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redis))
            services.AddStackExchangeRedisCache(o => { o.Configuration = redis; o.InstanceName = "gp:"; });
        else
            services.AddDistributedMemoryCache();
        services.AddSingleton<ICacheService, DistributedCacheService>();

        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<ITokenIssuer, JwtTokenIssuer>();

        services.AddHttpClient<ILlmClient, AnthropicLlmClient>((sp, http) =>
        {
            var o = sp.GetRequiredService<IOptions<LlmOptions>>().Value;
            http.BaseAddress = new Uri(o.BaseUrl);
            http.Timeout = TimeSpan.FromSeconds(o.TimeoutSeconds + 3);
        });

        services.AddSingleton<IExternalIdentityVerifier>(sp =>
            new OidcIdTokenVerifier("google", "https://accounts.google.com", sp.GetRequiredService<IOptions<OidcProviderOptions>>().Value.GoogleClientId));
        services.AddSingleton<IExternalIdentityVerifier>(sp =>
            new OidcIdTokenVerifier("apple", "https://appleid.apple.com", sp.GetRequiredService<IOptions<OidcProviderOptions>>().Value.AppleClientId));
        if (config.GetValue<bool>("Auth:EnableDevProvider"))
            services.AddSingleton<IExternalIdentityVerifier, DevIdentityVerifier>();

        return services;
    }

    /// <summary>Creates the schema (migrations when present, otherwise EnsureCreated for local dev) and seeds the demo catalog.</summary>
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var log = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();
        var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();

        if (db.Database.GetMigrations().Any()) await db.Database.MigrateAsync(ct);
        else await db.Database.EnsureCreatedAsync(ct);

        if (env.IsProduction() || await db.CatalogIdeas.AnyAsync(ct)) return;

        var ideas = CatalogSeed.Ideas();
        var partners = CatalogSeed.PartnerList();
        db.CatalogIdeas.AddRange(ideas);
        db.Partners.AddRange(partners);
        db.Offers.AddRange(CatalogSeed.Offers(ideas, partners));
        await db.SaveChangesAsync(ct);
        log.LogInformation("Seeded demo catalog: {Ideas} ideas, {Partners} partners", ideas.Count, partners.Count);
    }
}
