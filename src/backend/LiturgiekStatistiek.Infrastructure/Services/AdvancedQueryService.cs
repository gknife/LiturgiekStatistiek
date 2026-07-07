using LiturgiekStatistiek.Application.DTOs.Queries;
using LiturgiekStatistiek.Application.Interfaces;
using LiturgiekStatistiek.Domain.Entities;
using LiturgiekStatistiek.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LiturgiekStatistiek.Infrastructure.Services;

/// <summary>
/// Translates user-built "advanced query" definitions into safe EF Core LINQ.
/// Only whitelisted fields/operators are honoured; nothing is used to build raw
/// SQL, so the endpoint is safe to expose anonymously. Song-sequence operators
/// (which depend on element/song ordering) are evaluated in memory after the
/// EF-translatable filters have reduced the candidate set.
/// </summary>
public class AdvancedQueryService : IAdvancedQueryService
{
    private const int AggregateCap = 100;
    private readonly IApplicationDbContext _db;

    public AdvancedQueryService(IApplicationDbContext db)
    {
        _db = db;
    }

    public AdvancedQuerySchema GetSchema()
    {
        var fields = new List<AdvancedField>
        {
            new() { Key = "date", Label = "Datum", Type = "date", Operators = new() { "between", "before", "after", "eq" } },
            new() { Key = "congregation", Label = "Gemeente", Type = "congregation", Operators = new() { "eq", "in" }, CanGroupBy = true },
            new() { Key = "city", Label = "Plaats", Type = "text", Operators = new() { "eq", "contains" }, CanGroupBy = true },
            new() { Key = "denomination", Label = "Kerkgenootschap", Type = "text", Operators = new() { "eq", "contains" }, CanGroupBy = true },
            new() { Key = "preacher", Label = "Voorganger", Type = "preacher", Operators = new() { "eq" }, CanGroupBy = true },
            new() { Key = "timeOfDay", Label = "Dagdeel", Type = "timeOfDay", Operators = new() { "eq", "in" }, CanGroupBy = true },
            new() { Key = "bibleTranslation", Label = "Bijbelvertaling", Type = "text", Operators = new() { "eq", "contains" }, CanGroupBy = true },
            new() { Key = "specialOccasion", Label = "Bijzondere gelegenheid", Type = "text", Operators = new() { "eq", "contains" }, CanGroupBy = true },
            new() { Key = "isReadingService", Label = "Leesdienst", Type = "bool", Operators = new() { "isTrue", "isFalse" } },
            new() { Key = "beurtzang", Label = "Beurtzang", Type = "bool", Operators = new() { "isTrue", "isFalse" } },
            new() { Key = "performer", Label = "Wie doet onderdeel", Type = "text", Operators = new() { "eq", "contains" } },
            new() { Key = "sermonTheme", Label = "Preekthema", Type = "text", Operators = new() { "contains" } },
            new() { Key = "sermonText", Label = "Preektekst", Type = "text", Operators = new() { "contains" } },
            new() { Key = "songUsed", Label = "Lied gebruikt", Type = "song", Operators = new() { "eq" } },
            new() { Key = "songComplete", Label = "Lied volledig gezongen", Type = "song", Operators = new() { "eq" } },
            new() { Key = "songSequence", Label = "Liedvolgorde", Type = "songSequence", Operators = new() { "seqBefore", "seqAfter", "seqDirectlyBefore", "seqDirectlyAfter" } },
        };

        var groupBy = new List<AdvancedField>
        {
            new() { Key = "congregation", Label = "Gemeente" },
            new() { Key = "city", Label = "Plaats" },
            new() { Key = "denomination", Label = "Kerkgenootschap" },
            new() { Key = "preacher", Label = "Voorganger" },
            new() { Key = "timeOfDay", Label = "Dagdeel" },
            new() { Key = "bibleTranslation", Label = "Bijbelvertaling" },
            new() { Key = "specialOccasion", Label = "Bijzondere gelegenheid" },
            new() { Key = "year", Label = "Jaar" },
        };

        return new AdvancedQuerySchema { Fields = fields, GroupByFields = groupBy };
    }

    public async Task<QueryResult> ExecuteAsync(AdvancedQueryDefinition definition, CancellationToken ct = default)
    {
        var services = await GetFilteredServicesAsync(definition.Filters, ct);

        if (string.Equals(definition.OutputMode, "aggregate", StringComparison.OrdinalIgnoreCase))
            return BuildAggregateResult(definition, services);

        return BuildListResult(definition, services);
    }

    public async Task<QueryResult> CompareAsync(IReadOnlyList<AdvancedQueryDefinition> definitions, CancellationToken ct = default)
    {
        if (definitions.Count == 0)
            return new QueryResult { Title = "Vergelijking", Description = "Geen queries opgegeven." };

        var allAggregate = definitions.All(d => string.Equals(d.OutputMode, "aggregate", StringComparison.OrdinalIgnoreCase));
        var sharedGroupBy = allAggregate
            && definitions.Select(d => d.GroupBy).Distinct(StringComparer.OrdinalIgnoreCase).Count() == 1
            && !string.IsNullOrWhiteSpace(definitions[0].GroupBy);

        var palette = new[] { "#3f51b5", "#e91e63", "#4caf50", "#ff9800", "#9c27b0", "#00bcd4", "#795548", "#607d8b" };

        if (sharedGroupBy)
        {
            // Aligned multi-dataset comparison over the union of group labels.
            var perQuery = new List<(string Name, Dictionary<string, int> Counts)>();
            for (var i = 0; i < definitions.Count; i++)
            {
                var def = definitions[i];
                var services = await GetFilteredServicesAsync(def.Filters, ct);
                var counts = Aggregate(def.GroupBy!, services)
                    .ToDictionary(x => x.Label, x => x.Count, StringComparer.OrdinalIgnoreCase);
                perQuery.Add((QueryName(def, i), counts));
            }

            var labels = perQuery
                .SelectMany(q => q.Counts.Keys)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(l => l)
                .Take(AggregateCap)
                .ToList();

            var columns = new List<string> { "Groep" };
            columns.AddRange(perQuery.Select(q => q.Name));

            var rows = labels.Select(label =>
            {
                var row = new Dictionary<string, object?> { ["Groep"] = label };
                foreach (var q in perQuery)
                    row[q.Name] = q.Counts.TryGetValue(label, out var c) ? c : 0;
                return row;
            }).ToList();

            var datasets = perQuery.Select((q, i) => new ChartDataset
            {
                Label = q.Name,
                Data = labels.Select(l => q.Counts.TryGetValue(l, out var c) ? (double)c : 0d).ToList(),
                BackgroundColor = palette[i % palette.Length]
            }).ToList();

            return new QueryResult
            {
                Title = "Vergelijking van queries",
                Description = $"{definitions.Count} queries vergeleken, gegroepeerd op {definitions[0].GroupBy}.",
                ChartType = definitions[0].ChartType,
                Columns = columns,
                Rows = rows,
                TotalCount = labels.Count,
                Chart = new ChartData { Labels = labels, Datasets = datasets }
            };
        }

        // Fallback: compare the number of matching services per query.
        var compareRows = new List<Dictionary<string, object?>>();
        var compareLabels = new List<string>();
        var compareData = new List<double>();
        for (var i = 0; i < definitions.Count; i++)
        {
            var def = definitions[i];
            var services = await GetFilteredServicesAsync(def.Filters, ct);
            var name = QueryName(def, i);
            compareRows.Add(new Dictionary<string, object?> { ["Query"] = name, ["Aantal diensten"] = services.Count });
            compareLabels.Add(name);
            compareData.Add(services.Count);
        }

        return new QueryResult
        {
            Title = "Vergelijking van queries",
            Description = $"{definitions.Count} queries vergeleken op aantal gevonden diensten.",
            ChartType = "bar",
            Columns = new() { "Query", "Aantal diensten" },
            Rows = compareRows,
            TotalCount = definitions.Count,
            Chart = new ChartData
            {
                Labels = compareLabels,
                Datasets = new() { new() { Label = "Aantal diensten", Data = compareData, BackgroundColor = palette[0] } }
            }
        };
    }

    private static string QueryName(AdvancedQueryDefinition def, int index)
        => string.IsNullOrWhiteSpace(def.Name) ? $"Query {index + 1}" : def.Name!;

    private async Task<List<Service>> GetFilteredServicesAsync(IEnumerable<AdvancedFilter> filters, CancellationToken ct)
    {
        var sequenceFilters = new List<AdvancedFilter>();
        var completeFilters = new List<AdvancedFilter>();

        IQueryable<Service> query = _db.Services
            .Where(s => s.Status == ServiceStatus.Gepubliceerd)
            .Include(s => s.Congregation).ThenInclude(c => c.Denomination)
            .Include(s => s.Preacher)
            .Include(s => s.SpecialOccasion)
            .Include(s => s.Elements).ThenInclude(e => e.BibleTranslation)
            .Include(s => s.Elements).ThenInclude(e => e.Performer)
            .Include(s => s.Elements).ThenInclude(e => e.Songs).ThenInclude(x => x.Bundle)
            .Include(s => s.Elements).ThenInclude(e => e.Songs).ThenInclude(x => x.Verses);

        foreach (var filter in filters ?? Enumerable.Empty<AdvancedFilter>())
        {
            if (string.Equals(filter.Field, "songSequence", StringComparison.OrdinalIgnoreCase))
            {
                sequenceFilters.Add(filter);
                continue;
            }

            if (string.Equals(filter.Field, "songComplete", StringComparison.OrdinalIgnoreCase))
            {
                completeFilters.Add(filter);
                continue;
            }

            query = ApplyFilter(query, filter);
        }

        var services = await query.ToListAsync(ct);

        if (sequenceFilters.Count > 0)
            services = services.Where(s => sequenceFilters.All(f => MatchesSequence(s, f))).ToList();

        if (completeFilters.Count > 0)
        {
            var catalog = await _db.Songs
                .Select(s => new { s.BundleId, s.Section, s.Number, s.NumberOfVerses, BundleValue = s.Bundle.Value, BundleAbbr = s.Bundle.Abbreviation })
                .ToListAsync(ct);
            services = services.Where(s => completeFilters.All(f => MatchesComplete(s, f, catalog))).ToList();
        }

        return services;
    }

    private static bool MatchesComplete(Service s, AdvancedFilter f, IEnumerable<dynamic> catalog)
    {
        if (string.IsNullOrWhiteSpace(f.SongBundleA) || !f.SongNumberA.HasValue) return false;
        var bundle = f.SongBundleA!;
        var number = f.SongNumberA!.Value;

        var matchingSongs = s.Elements
            .SelectMany(e => e.Songs)
            .Where(x => (x.Bundle.Value == bundle || x.Bundle.Abbreviation == bundle) && x.SongNumber == number)
            .ToList();
        if (matchingSongs.Count == 0) return false;

        int? catalogVerses = null;
        foreach (var c in catalog)
        {
            if ((c.BundleValue == bundle || c.BundleAbbr == bundle) && c.Number == number)
            {
                catalogVerses = (int?)c.NumberOfVerses;
                break;
            }
        }

        var serviceVerses = matchingSongs.SelectMany(x => x.Verses.Select(v => v.VerseLabel));
        var elementVerses = matchingSongs
            .GroupBy(x => x.ServiceElementId)
            .Select(g => g.SelectMany(x => x.Verses.Select(v => v.VerseLabel)).ToList())
            .OrderByDescending(l => l.Count)
            .First();

        var comp = Application.Services.SongCompletenessCalculator.Compute(catalogVerses, elementVerses, serviceVerses);
        return comp.CompleteInService;
    }

    private static IQueryable<Service> ApplyFilter(IQueryable<Service> query, AdvancedFilter f)
    {
        var op = f.Operator?.ToLowerInvariant() ?? "eq";
        switch (f.Field?.ToLowerInvariant())
        {
            case "date":
                var d1 = ParseDate(f.Value);
                if (op == "between")
                {
                    var d2 = ParseDate(f.Value2);
                    if (d1.HasValue) query = query.Where(s => s.Date >= d1.Value);
                    if (d2.HasValue) query = query.Where(s => s.Date <= d2.Value);
                }
                else if (op == "before" && d1.HasValue) query = query.Where(s => s.Date < d1.Value);
                else if (op == "after" && d1.HasValue) query = query.Where(s => s.Date > d1.Value);
                else if (op == "eq" && d1.HasValue) query = query.Where(s => s.Date == d1.Value);
                return query;

            case "congregation":
                if (op == "in")
                {
                    var ids = ParseGuids(f.Value);
                    if (ids.Count > 0) query = query.Where(s => ids.Contains(s.CongregationId));
                }
                else if (Guid.TryParse(f.Value, out var cid))
                {
                    query = op == "neq"
                        ? query.Where(s => s.CongregationId != cid)
                        : query.Where(s => s.CongregationId == cid);
                }
                return query;

            case "city":
                if (!string.IsNullOrWhiteSpace(f.Value))
                    query = op == "contains"
                        ? query.Where(s => s.Congregation.City.Contains(f.Value))
                        : query.Where(s => s.Congregation.City == f.Value);
                return query;

            case "denomination":
                if (!string.IsNullOrWhiteSpace(f.Value))
                    query = op == "contains"
                        ? query.Where(s => s.Congregation.Denomination != null && s.Congregation.Denomination.Value.Contains(f.Value))
                        : query.Where(s => s.Congregation.Denomination != null && s.Congregation.Denomination.Value == f.Value);
                return query;

            case "preacher":
                if (Guid.TryParse(f.Value, out var pid))
                    query = query.Where(s => s.PreacherId == pid);
                return query;

            case "timeofday":
                if (op == "in")
                {
                    var times = ParseTimes(f.Value);
                    if (times.Count > 0) query = query.Where(s => times.Contains(s.TimeOfDay));
                }
                else if (TryParseTime(f.Value, out var tod))
                {
                    query = query.Where(s => s.TimeOfDay == tod);
                }
                return query;

            case "bibletranslation":
                if (!string.IsNullOrWhiteSpace(f.Value))
                    query = op == "contains"
                        ? query.Where(s => s.Elements.Any(e => e.BibleTranslation != null && e.BibleTranslation.Value.Contains(f.Value)))
                        : query.Where(s => s.Elements.Any(e => e.BibleTranslation != null && e.BibleTranslation.Value == f.Value));
                return query;

            case "beurtzang":
                var beurtzangTrue = op != "isfalse";
                query = query.Where(s => s.Elements.Any(e => e.IsBeurtzang) == beurtzangTrue);
                return query;

            case "performer":
                if (!string.IsNullOrWhiteSpace(f.Value))
                    query = op == "contains"
                        ? query.Where(s => s.Elements.Any(e => e.Performer != null && e.Performer.Value.Contains(f.Value)))
                        : query.Where(s => s.Elements.Any(e => e.Performer != null && e.Performer.Value == f.Value));
                return query;

            case "specialoccasion":
                if (!string.IsNullOrWhiteSpace(f.Value))
                    query = op == "contains"
                        ? query.Where(s => s.SpecialOccasion != null && s.SpecialOccasion.Value.Contains(f.Value))
                        : query.Where(s => s.SpecialOccasion != null && s.SpecialOccasion.Value == f.Value);
                return query;

            case "isreadingservice":
                var wantTrue = op != "isfalse";
                query = query.Where(s => s.IsReadingService == wantTrue);
                return query;

            case "sermontheme":
                if (!string.IsNullOrWhiteSpace(f.Value))
                    query = query.Where(s => s.SermonTheme != null && s.SermonTheme.Contains(f.Value));
                return query;

            case "sermontext":
                if (!string.IsNullOrWhiteSpace(f.Value))
                    query = query.Where(s => s.SermonText != null && s.SermonText.Contains(f.Value));
                return query;

            case "songused":
                if (!string.IsNullOrWhiteSpace(f.SongBundleA) && f.SongNumberA.HasValue)
                {
                    var bundle = f.SongBundleA;
                    var number = f.SongNumberA.Value;
                    query = query.Where(s => s.Elements.Any(e =>
                        e.Songs.Any(x => (x.Bundle.Value == bundle || x.Bundle.Abbreviation == bundle) && x.SongNumber == number)));
                }
                return query;

            default:
                // Unknown / unsupported field is ignored (whitelist behaviour).
                return query;
        }
    }

    private QueryResult BuildListResult(AdvancedQueryDefinition def, List<Service> services)
    {
        var ordered = services.OrderByDescending(s => s.Date).ToList();
        var total = ordered.Count;
        var pageSize = def.PageSize <= 0 ? 50 : Math.Min(def.PageSize, 200);
        var page = def.Page <= 0 ? 1 : def.Page;

        var rows = ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ProjectServiceRow)
            .ToList();

        return new QueryResult
        {
            Title = string.IsNullOrWhiteSpace(def.Name) ? "Geavanceerde query" : def.Name!,
            Description = $"{total} diensten gevonden.",
            ChartType = "table",
            Columns = new() { "Datum", "Gemeente", "Plaats", "Dagdeel", "Voorganger", "Bijbelvertaling", "Bijzondere gelegenheid" },
            Rows = rows,
            TotalCount = total
        };
    }

    private QueryResult BuildAggregateResult(AdvancedQueryDefinition def, List<Service> services)
    {
        var groupBy = def.GroupBy ?? "congregation";
        var groups = Aggregate(groupBy, services)
            .OrderByDescending(x => x.Count)
            .Take(AggregateCap)
            .ToList();

        return new QueryResult
        {
            Title = string.IsNullOrWhiteSpace(def.Name) ? "Geavanceerde query (aggregaat)" : def.Name!,
            Description = $"Gegroepeerd op {groupBy}; {groups.Count} groepen.",
            ChartType = def.ChartType,
            Columns = new() { "Groep", "Aantal" },
            Rows = groups.Select(g => new Dictionary<string, object?>
            {
                ["Groep"] = g.Label,
                ["Aantal"] = g.Count
            }).ToList(),
            TotalCount = groups.Count,
            Chart = new ChartData
            {
                Labels = groups.Select(g => g.Label).ToList(),
                Datasets = new() { new() { Label = "Aantal diensten", Data = groups.Select(g => (double)g.Count).ToList() } }
            }
        };
    }

    private static List<(string Label, int Count)> Aggregate(string groupBy, IEnumerable<Service> services)
    {
        Func<Service, string> key = groupBy.ToLowerInvariant() switch
        {
            "congregation" => s => s.Congregation?.Name ?? "(onbekend)",
            "city" => s => string.IsNullOrWhiteSpace(s.Congregation?.City) ? "(onbekend)" : s.Congregation!.City,
            "denomination" => s => s.Congregation?.Denomination?.Value ?? "(onbekend)",
            "preacher" => s => s.Preacher?.FullName ?? "(onbekend)",
            "timeofday" => s => TimeOfDayLabel(s.TimeOfDay),
            "bibletranslation" => s => s.Elements
                .Where(e => e.BibleTranslation != null)
                .Select(e => e.BibleTranslation!.Value)
                .FirstOrDefault() ?? "(onbekend)",
            "specialoccasion" => s => s.SpecialOccasion?.Value ?? "(geen)",
            "year" => s => s.Date.Year.ToString(),
            _ => s => s.Congregation?.Name ?? "(onbekend)"
        };

        return services
            .GroupBy(key)
            .Select(g => (Label: g.Key, Count: g.Count()))
            .ToList();
    }

    private static Dictionary<string, object?> ProjectServiceRow(Service s) => new()
    {
        ["Datum"] = s.Date.ToString("yyyy-MM-dd"),
        ["Gemeente"] = s.Congregation?.Name ?? string.Empty,
        ["Plaats"] = s.Congregation?.City ?? string.Empty,
        ["Dagdeel"] = TimeOfDayLabel(s.TimeOfDay),
        ["Voorganger"] = s.Preacher?.FullName ?? string.Empty,
        ["Bijbelvertaling"] = string.Join("; ", s.Elements
            .Where(e => e.BibleTranslation != null)
            .Select(e => e.BibleTranslation!.Value)
            .Distinct()),
        ["Bijzondere gelegenheid"] = s.SpecialOccasion?.Value ?? string.Empty,
    };

    private static bool MatchesSequence(Service s, AdvancedFilter f)
    {
        if (string.IsNullOrWhiteSpace(f.SongBundleA) || !f.SongNumberA.HasValue
            || string.IsNullOrWhiteSpace(f.SongBundleB) || !f.SongNumberB.HasValue)
            return false;

        var ordered = s.Elements
            .OrderBy(e => e.Position)
            .SelectMany(e => e.Songs.OrderBy(x => x.Position))
            .ToList();

        bool IsA(ServiceElementSong x) => SongMatches(x, f.SongBundleA!, f.SongNumberA!.Value);
        bool IsB(ServiceElementSong x) => SongMatches(x, f.SongBundleB!, f.SongNumberB!.Value);

        var op = f.Operator?.ToLowerInvariant() ?? "seqbefore";
        switch (op)
        {
            case "seqbefore":
                for (var i = 0; i < ordered.Count; i++)
                    if (IsA(ordered[i]))
                        for (var j = i + 1; j < ordered.Count; j++)
                            if (IsB(ordered[j])) return true;
                return false;
            case "seqafter":
                for (var i = 0; i < ordered.Count; i++)
                    if (IsB(ordered[i]))
                        for (var j = i + 1; j < ordered.Count; j++)
                            if (IsA(ordered[j])) return true;
                return false;
            case "seqdirectlybefore":
                for (var i = 0; i < ordered.Count - 1; i++)
                    if (IsA(ordered[i]) && IsB(ordered[i + 1])) return true;
                return false;
            case "seqdirectlyafter":
                for (var i = 0; i < ordered.Count - 1; i++)
                    if (IsB(ordered[i]) && IsA(ordered[i + 1])) return true;
                return false;
            default:
                return false;
        }
    }

    private static bool SongMatches(ServiceElementSong x, string bundle, int number)
        => x.SongNumber == number
           && (string.Equals(x.Bundle?.Value, bundle, StringComparison.OrdinalIgnoreCase)
               || string.Equals(x.Bundle?.Abbreviation, bundle, StringComparison.OrdinalIgnoreCase));

    private static string TimeOfDayLabel(TimeOfDay t) => t switch
    {
        TimeOfDay.Morning => "Morgen",
        TimeOfDay.Afternoon => "Middag",
        TimeOfDay.Evening => "Avond",
        _ => t.ToString()
    };

    private static DateOnly? ParseDate(string? value)
        => DateOnly.TryParse(value, out var d) ? d : null;

    private static List<Guid> ParseGuids(string? value)
        => (value ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(v => Guid.TryParse(v, out var g) ? g : (Guid?)null)
            .Where(g => g.HasValue)
            .Select(g => g!.Value)
            .ToList();

    private static bool TryParseTime(string? value, out TimeOfDay time)
    {
        if (int.TryParse(value, out var i) && Enum.IsDefined(typeof(TimeOfDay), i))
        {
            time = (TimeOfDay)i;
            return true;
        }
        return Enum.TryParse(value, true, out time);
    }

    private static List<TimeOfDay> ParseTimes(string? value)
        => (value ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(v => TryParseTime(v, out var t) ? t : (TimeOfDay?)null)
            .Where(t => t.HasValue)
            .Select(t => t!.Value)
            .ToList();
}
