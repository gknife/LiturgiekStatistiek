using LiturgiekStatistiek.Domain.Entities;
using LiturgiekStatistiek.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LiturgiekStatistiek.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Congregation> Congregations => Set<Congregation>();
    public DbSet<Preacher> Preachers => Set<Preacher>();
    public DbSet<CongregationPreacher> CongregationPreachers => Set<CongregationPreacher>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<ServiceElement> ServiceElements => Set<ServiceElement>();
    public DbSet<ServiceElementSong> ServiceElementSongs => Set<ServiceElementSong>();
    public DbSet<SongVerse> SongVerses => Set<SongVerse>();
    public DbSet<ServiceBundle> ServiceBundles => Set<ServiceBundle>();
    public DbSet<ServiceMetadata> ServiceMetadata => Set<ServiceMetadata>();
    public DbSet<ListDefinition> ListDefinitions => Set<ListDefinition>();
    public DbSet<ListItem> ListItems => Set<ListItem>();
    public DbSet<Song> Songs => Set<Song>();
    public DbSet<SongCatalogVerse> SongCatalogVerses => Set<SongCatalogVerse>();
    public DbSet<BundleSection> BundleSections => Set<BundleSection>();
    public DbSet<ContentPage> ContentPages => Set<ContentPage>();
    public DbSet<SavedQuery> SavedQueries => Set<SavedQuery>();
    public DbSet<ChangeHistory> ChangeHistory => Set<ChangeHistory>();
    public DbSet<UserSetting> UserSettings => Set<UserSetting>();
    public DbSet<RecentSearch> RecentSearches => Set<RecentSearch>();
    public DbSet<BibleBook> BibleBooks => Set<BibleBook>();
    public DbSet<BibleBookTranslationName> BibleBookTranslationNames => Set<BibleBookTranslationName>();
    public DbSet<SermonTextReference> SermonTextReferences => Set<SermonTextReference>();
    public DbSet<ReadingReference> ReadingReferences => Set<ReadingReference>();
    public DbSet<ServiceTemplate> ServiceTemplates => Set<ServiceTemplate>();
    public DbSet<ServiceTemplateElement> ServiceTemplateElements => Set<ServiceTemplateElement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity is IHasAuditFields auditable)
                {
                    auditable.CreatedAt = DateTime.UtcNow;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                if (entry.Entity is IHasAuditFields auditable)
                {
                    auditable.ModifiedAt = DateTime.UtcNow;
                }
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
