using LiturgiekStatistiek.Application.Interfaces;
using LiturgiekStatistiek.Domain.Interfaces;
using LiturgiekStatistiek.Infrastructure.Persistence;
using LiturgiekStatistiek.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LiturgiekStatistiek.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, bool useInMemory = false)
    {
        if (useInMemory)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase("LiturgiekStatistiek"));
        }
        else
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));
        }

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<IServiceService, ServiceService>();
        services.AddScoped<ICongregationService, CongregationService>();
        services.AddScoped<IPreacherService, PreacherService>();
        services.AddScoped<IListService, ListService>();
        services.AddScoped<ISongService, SongService>();
        services.AddScoped<IContentService, ContentService>();
        services.AddScoped<IQueryService, QueryService>();
        services.AddSingleton<ILlmService, LlmService>();

        return services;
    }
}
