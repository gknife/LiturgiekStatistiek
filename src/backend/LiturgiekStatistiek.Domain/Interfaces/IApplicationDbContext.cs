using LiturgiekStatistiek.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LiturgiekStatistiek.Domain.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Congregation> Congregations { get; }
    DbSet<Preacher> Preachers { get; }
    DbSet<Service> Services { get; }
    DbSet<ServiceElement> ServiceElements { get; }
    DbSet<ServiceElementSong> ServiceElementSongs { get; }
    DbSet<SongVerse> SongVerses { get; }
    DbSet<ServiceBundle> ServiceBundles { get; }
    DbSet<ServiceMetadata> ServiceMetadata { get; }
    DbSet<ListDefinition> ListDefinitions { get; }
    DbSet<ListItem> ListItems { get; }
    DbSet<Song> Songs { get; }
    DbSet<ContentPage> ContentPages { get; }
    DbSet<SavedQuery> SavedQueries { get; }
    DbSet<ChangeHistory> ChangeHistory { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
