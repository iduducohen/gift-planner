using System.Text.Json;
using GiftPlanner.Application.Common;
using GiftPlanner.Domain.Catalog;
using GiftPlanner.Domain.GiftSessions;
using GiftPlanner.Domain.Ideas;
using GiftPlanner.Domain.Orders;
using GiftPlanner.Domain.Plans;
using GiftPlanner.Domain.Recipients;
using GiftPlanner.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GiftPlanner.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Recipient> Recipients => Set<Recipient>();
    public DbSet<GiftSession> GiftSessions => Set<GiftSession>();
    public DbSet<IdeaSet> IdeaSets => Set<IdeaSet>();
    public DbSet<Recommendation> Recommendations => Set<Recommendation>();
    public DbSet<SavedIdea> SavedIdeas => Set<SavedIdea>();
    public DbSet<SurprisePlan> Plans => Set<SurprisePlan>();
    public DbSet<PurchaseIntent> PurchaseIntents => Set<PurchaseIntent>();
    public DbSet<CatalogIdea> CatalogIdeas => Set<CatalogIdea>();
    public DbSet<Partner> Partners => Set<Partner>();
    public DbSet<Offer> Offers => Set<Offer>();

    Task IUnitOfWork.SaveChangesAsync(CancellationToken ct) => SaveChangesAsync(ct);

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}

internal static class Json
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    /// <summary>Stores an immutable value object as jsonb. Values are replaced, never mutated, so reference comparison is enough.</summary>
    public static PropertyBuilder<T> HasJsonConversion<T>(this PropertyBuilder<T> p) where T : class =>
        p.HasConversion(
                v => JsonSerializer.Serialize(v, Options),
                v => JsonSerializer.Deserialize<T>(v, Options)!)
            .HasColumnType("jsonb");

    public static PropertyBuilder<Dictionary<string, double>> HasJsonDictionary(this PropertyBuilder<Dictionary<string, double>> p) =>
        p.HasConversion(
                v => JsonSerializer.Serialize(v, Options),
                v => JsonSerializer.Deserialize<Dictionary<string, double>>(v, Options) ?? new(),
                new ValueComparer<Dictionary<string, double>>(
                    (a, c) => a != null && c != null && a.Count == c.Count && !a.Except(c).Any(),
                    v => v.Aggregate(0, (h, kv) => HashCode.Combine(h, kv.Key, kv.Value)),
                    v => new Dictionary<string, double>(v)))
            .HasColumnType("jsonb");
}

internal sealed class UserConfig : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");
        b.HasKey(x => x.Id);
        b.Property(x => x.ExternalProvider).HasMaxLength(20);
        b.Property(x => x.ExternalId).HasMaxLength(200);
        b.Property(x => x.DisplayName).HasMaxLength(100);
        b.HasIndex(x => new { x.ExternalProvider, x.ExternalId }).IsUnique();
        b.Ignore(x => x.IsAnonymous);
    }
}

internal sealed class RecipientConfig : IEntityTypeConfiguration<Recipient>
{
    public void Configure(EntityTypeBuilder<Recipient> b)
    {
        b.ToTable("recipients");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.UserId);
        b.Property(x => x.Nickname).HasMaxLength(40).IsRequired();
        b.Property(x => x.RelationshipKey).HasMaxLength(40);
        b.Property(x => x.AgeRangeKey).HasMaxLength(20);
        b.Property(x => x.Notes).HasMaxLength(500);

        b.OwnsMany(x => x.Occasions, o =>
        {
            o.ToTable("recipient_occasions");
            o.WithOwner().HasForeignKey("RecipientId");
            o.HasKey(x => x.Id);
            o.Property(x => x.Id).ValueGeneratedNever();
            o.Property(x => x.TypeKey).HasMaxLength(40);
        });
        b.Navigation(x => x.Occasions).UsePropertyAccessMode(PropertyAccessMode.Field);

        b.OwnsMany(x => x.History, h =>
        {
            h.ToTable("recipient_gift_history");
            h.WithOwner().HasForeignKey("RecipientId");
            h.HasKey(x => x.Id);
            h.Property(x => x.Id).ValueGeneratedNever();
            h.Property(x => x.Title).HasMaxLength(120);
        });
        b.Navigation(x => x.History).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class GiftSessionConfig : IEntityTypeConfiguration<GiftSession>
{
    public void Configure(EntityTypeBuilder<GiftSession> b)
    {
        b.ToTable("gift_sessions");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.UserId);
        b.Property(x => x.Brief).HasJsonConversion();
        b.Property(x => x.Question).HasJsonConversion();
        b.Property(x => x.Answer).HasMaxLength(40);
    }
}

internal sealed class IdeaSetConfig : IEntityTypeConfiguration<IdeaSet>
{
    public void Configure(EntityTypeBuilder<IdeaSet> b)
    {
        b.ToTable("idea_sets");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.UserId, x.SessionId });
        b.Property(x => x.RefinePreset).HasMaxLength(40);
        b.Property(x => x.RefineText).HasMaxLength(300);
        b.Property(x => x.ComposerName).HasMaxLength(80);
        b.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.IdeaSetId).OnDelete(DeleteBehavior.Cascade);
        b.Navigation(x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class RecommendationConfig : IEntityTypeConfiguration<Recommendation>
{
    public void Configure(EntityTypeBuilder<Recommendation> b)
    {
        b.ToTable("recommendations");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Title).HasMaxLength(120);
        b.Property(x => x.Why).HasMaxLength(400);
        b.Property(x => x.EmotionalValue).HasMaxLength(200);
        b.Property(x => x.HowTo).HasMaxLength(200);
        b.Property(x => x.Upgrade).HasMaxLength(200);
        b.Property(x => x.IconKey).HasMaxLength(30);
        b.Property(x => x.ScoreBreakdown).HasJsonDictionary();
    }
}

internal sealed class SavedIdeaConfig : IEntityTypeConfiguration<SavedIdea>
{
    public void Configure(EntityTypeBuilder<SavedIdea> b)
    {
        b.ToTable("saved_ideas");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.UserId, x.RecommendationId }).IsUnique();
        b.HasOne<Recommendation>().WithMany().HasForeignKey(x => x.RecommendationId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class PlanConfig : IEntityTypeConfiguration<SurprisePlan>
{
    public void Configure(EntityTypeBuilder<SurprisePlan> b)
    {
        b.ToTable("plans");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.UserId, x.RecommendationId }).IsUnique();
        b.Property(x => x.Title).HasMaxLength(120);
        b.Property(x => x.PlanB).HasMaxLength(300);

        b.OwnsMany(x => x.Steps, s =>
        {
            s.ToTable("plan_steps");
            s.WithOwner().HasForeignKey("PlanId");
            s.Property<int>("Id");
            s.HasKey("Id");
            s.Property(x => x.Title).HasMaxLength(120);
            s.Property(x => x.Detail).HasMaxLength(200);
        });
        b.Navigation(x => x.Steps).UsePropertyAccessMode(PropertyAccessMode.Field);

        b.OwnsMany(x => x.Tasks, t =>
        {
            t.ToTable("plan_tasks");
            t.WithOwner().HasForeignKey("PlanId");
            t.HasKey(x => x.Id);
            t.Property(x => x.Id).ValueGeneratedNever();
            t.Property(x => x.Title).HasMaxLength(120);
        });
        b.Navigation(x => x.Tasks).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class PurchaseIntentConfig : IEntityTypeConfiguration<PurchaseIntent>
{
    public void Configure(EntityTypeBuilder<PurchaseIntent> b)
    {
        b.ToTable("purchase_intents");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.UserId);
        b.HasIndex(x => x.PartnerId);
        b.Property(x => x.CouponCode).HasMaxLength(40);
    }
}

internal sealed class CatalogConfig : IEntityTypeConfiguration<CatalogIdea>, IEntityTypeConfiguration<Partner>, IEntityTypeConfiguration<Offer>
{
    public void Configure(EntityTypeBuilder<CatalogIdea> b)
    {
        b.ToTable("catalog_ideas");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Title).HasMaxLength(120);
        b.Property(x => x.Description).HasMaxLength(400);
        b.Property(x => x.IconKey).HasMaxLength(30);
        b.Property(x => x.Upgrade).HasMaxLength(200);
        b.HasIndex(x => x.Interests).HasMethod("gin");
    }

    public void Configure(EntityTypeBuilder<Partner> b)
    {
        b.ToTable("partners");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Name).HasMaxLength(120);
        b.Property(x => x.Region).HasMaxLength(60);
        b.Property(x => x.Website).HasMaxLength(300);
        b.Property(x => x.ImageUrl).HasMaxLength(500);
        b.Property(x => x.Phone).HasMaxLength(20);
    }

    public void Configure(EntityTypeBuilder<Offer> b)
    {
        b.ToTable("offers");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.HasIndex(x => x.CatalogIdeaId);
        b.Property(x => x.Url).HasMaxLength(500);
        b.Property(x => x.CouponCode).HasMaxLength(40);
        b.Ignore(x => x.FinalPrice);
        b.HasOne<CatalogIdea>().WithMany().HasForeignKey(x => x.CatalogIdeaId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Partner>().WithMany().HasForeignKey(x => x.PartnerId).OnDelete(DeleteBehavior.Restrict);
    }
}
