using LiturgiekStatistiek.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LiturgiekStatistiek.Domain.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Congregation> Congregations { get; }
    DbSet<Preacher> Preachers { get; }
    DbSet<CongregationPreacher> CongregationPreachers { get; }
    DbSet<Service> Services { get; }
    DbSet<ServiceElement> ServiceElements { get; }
    DbSet<ServiceElementSong> ServiceElementSongs { get; }
    DbSet<SongVerse> SongVerses { get; }
    DbSet<ServiceBundle> ServiceBundles { get; }
    DbSet<ServiceMetadata> ServiceMetadata { get; }
    DbSet<ListDefinition> ListDefinitions { get; }
    DbSet<ListItem> ListItems { get; }
    DbSet<Song> Songs { get; }
    DbSet<SongCatalogVerse> SongCatalogVerses { get; }
    DbSet<ContentPage> ContentPages { get; }
    DbSet<SavedQuery> SavedQueries { get; }
    DbSet<ChangeHistory> ChangeHistory { get; }
    DbSet<UserSetting> UserSettings { get; }
    DbSet<RecentSearch> RecentSearches { get; }
    DbSet<BibleBook> BibleBooks { get; }
    DbSet<BibleBookTranslationName> BibleBookTranslationNames { get; }
    DbSet<SermonTextReference> SermonTextReferences { get; }
    DbSet<ReadingReference> ReadingReferences { get; }
    DbSet<ServiceTemplate> ServiceTemplates { get; }
    DbSet<ServiceTemplateElement> ServiceTemplateElements { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
