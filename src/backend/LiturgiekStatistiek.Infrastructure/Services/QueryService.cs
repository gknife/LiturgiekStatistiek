using LiturgiekStatistiek.Application.DTOs.Queries;
using LiturgiekStatistiek.Application.Interfaces;
using LiturgiekStatistiek.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LiturgiekStatistiek.Infrastructure.Services;

public class QueryService : IQueryService
{
    private readonly IApplicationDbContext _db;

    public QueryService(IApplicationDbContext db)
    {
        _db = db;
    }

    public Task<List<QueryTemplate>> GetTemplatesAsync()
    {
        var templates = new List<QueryTemplate>
        {
            new()
            {
                Id = "most-sung-song",
                Title = "Meest gezongen lied in gemeente",
                Description = "Welk lied wordt het vaakst gezongen in een bepaalde gemeente?",
                DefaultChartType = "bar",
                Parameters = new()
                {
                    new() { Name = "congregationId", Label = "Gemeente", Type = "congregation", Required = true },
                    new() { Name = "fromDate", Label = "Vanaf", Type = "date", Required = false },
                    new() { Name = "toDate", Label = "Tot", Type = "date", Required = false },
                }
            },
            new()
            {
                Id = "most-sung-verse",
                Title = "Meest gezongen couplet",
                Description = "Welk couplet van welk lied is het afgelopen jaar het meest gezongen?",
                DefaultChartType = "bar",
                Parameters = new()
                {
                    new() { Name = "year", Label = "Jaar", Type = "string", Required = true, DefaultValue = DateTime.Now.Year.ToString() },
                    new() { Name = "bundleId", Label = "Bundel", Type = "bundle", Required = false },
                }
            },
            new()
            {
                Id = "most-opening-song",
                Title = "Meest gezongen lied aan begin dienst",
                Description = "Welk lied wordt het vaakst als openingslied gezongen?",
                DefaultChartType = "bar",
                Parameters = new()
                {
                    new() { Name = "fromDate", Label = "Vanaf", Type = "date", Required = false },
                    new() { Name = "toDate", Label = "Tot", Type = "date", Required = false },
                }
            },
            new()
            {
                Id = "average-songs-per-service",
                Title = "Gemiddeld aantal liederen per dienst",
                Description = "Hoeveel liederen/coupletten worden gemiddeld gezongen in een gemeente?",
                DefaultChartType = "bar",
                Parameters = new()
                {
                    new() { Name = "congregationId", Label = "Gemeente", Type = "congregation", Required = false },
                    new() { Name = "fromDate", Label = "Vanaf", Type = "date", Required = false },
                    new() { Name = "toDate", Label = "Tot", Type = "date", Required = false },
                }
            },
            new()
            {
                Id = "most-psalms-congregation",
                Title = "Welke gemeente zingt de meeste psalmen?",
                Description = "Vergelijk gemeenten op het percentage psalmen ten opzichte van gezangen.",
                DefaultChartType = "bar",
                Parameters = new()
                {
                    new() { Name = "fromDate", Label = "Vanaf", Type = "date", Required = false },
                    new() { Name = "toDate", Label = "Tot", Type = "date", Required = false },
                }
            },
            new()
            {
                Id = "song-by-city-map",
                Title = "Welke stad zingt lied X het meest?",
                Description = "Toon op de kaart welke steden een bepaald lied het vaakst zingen.",
                DefaultChartType = "map",
                Parameters = new()
                {
                    new() { Name = "bundleId", Label = "Bundel", Type = "bundle", Required = true },
                    new() { Name = "songNumber", Label = "Liednummer", Type = "string", Required = true },
                    new() { Name = "fromDate", Label = "Vanaf", Type = "date", Required = false },
                    new() { Name = "toDate", Label = "Tot", Type = "date", Required = false },
                }
            },
            new()
            {
                Id = "song-by-period",
                Title = "Welk lied wordt in periode X gezongen?",
                Description = "Welke liederen worden het meest gezongen in een bepaalde maand of seizoen?",
                DefaultChartType = "bar",
                Parameters = new()
                {
                    new() { Name = "month", Label = "Maand (1-12)", Type = "string", Required = false },
                    new() { Name = "year", Label = "Jaar", Type = "string", Required = true, DefaultValue = DateTime.Now.Year.ToString() },
                }
            },
            new()
            {
                Id = "services-with-song",
                Title = "Alle diensten met lied X",
                Description = "Geef alle diensten waarin een bepaald lied gezongen is.",
                DefaultChartType = "table",
                Parameters = new()
                {
                    new() { Name = "bundleId", Label = "Bundel", Type = "bundle", Required = true },
                    new() { Name = "songNumber", Label = "Liednummer", Type = "string", Required = true },
                }
            },
            new()
            {
                Id = "song-after-song",
                Title = "Lied X direct na lied Y",
                Description = "Geef alle diensten waarin lied X direct na lied Y gezongen is.",
                DefaultChartType = "table",
                Parameters = new()
                {
                    new() { Name = "bundleIdA", Label = "Bundel lied A", Type = "bundle", Required = true },
                    new() { Name = "songNumberA", Label = "Liednummer A", Type = "string", Required = true },
                    new() { Name = "bundleIdB", Label = "Bundel lied B", Type = "bundle", Required = true },
                    new() { Name = "songNumberB", Label = "Liednummer B", Type = "string", Required = true },
                }
            },
            new()
            {
                Id = "song-usage-over-time",
                Title = "Gebruik van lied X over tijd",
                Description = "Hoe is het gebruik van een bepaald lied toe- of afgenomen over de jaren?",
                DefaultChartType = "line",
                Parameters = new()
                {
                    new() { Name = "bundleId", Label = "Bundel", Type = "bundle", Required = true },
                    new() { Name = "songNumber", Label = "Liednummer", Type = "string", Required = true },
                }
            },
        };

        return Task.FromResult(templates);
    }

    public async Task<QueryResult> ExecuteTemplateAsync(string templateId, Dictionary<string, string> parameters, CancellationToken ct = default)
    {
        return templateId switch
        {
            "most-sung-song" => await MostSungSongAsync(parameters, ct),
            "most-sung-verse" => await MostSungVerseAsync(parameters, ct),
            "most-opening-song" => await MostOpeningSongAsync(parameters, ct),
            "average-songs-per-service" => await AverageSongsPerServiceAsync(parameters, ct),
            "most-psalms-congregation" => await MostPsalmsCongregationAsync(parameters, ct),
            "song-by-city-map" => await SongByCityMapAsync(parameters, ct),
            "song-by-period" => await SongByPeriodAsync(parameters, ct),
            "services-with-song" => await ServicesWithSongAsync(parameters, ct),
            "song-after-song" => await SongAfterSongAsync(parameters, ct),
            "song-usage-over-time" => await SongUsageOverTimeAsync(parameters, ct),
            _ => new QueryResult { Title = "Onbekende query", Description = $"Template '{templateId}' niet gevonden." }
        };
    }

    public Task<QueryResult> ExecuteNaturalLanguageAsync(string query, CancellationToken ct = default)
    {
        // TODO: Integrate with Azure OpenAI GPT-4o-mini
        return Task.FromResult(new QueryResult
        {
            Title = "Natuurlijke taalverwerking",
            Description = "Deze functie wordt binnenkort beschikbaar. Gebruik voorlopig de voorgedefinieerde sjablonen.",
        });
    }

    // --- Template implementations ---

    private async Task<QueryResult> MostSungSongAsync(Dictionary<string, string> parameters, CancellationToken ct)
    {
        var congregationId = Guid.Parse(parameters["congregationId"]);
        var query = _db.ServiceElementSongs
            .Include(ses => ses.ServiceElement)
                .ThenInclude(se => se.Service)
            .Include(ses => ses.Bundle)
            .Where(ses => ses.ServiceElement.Service.CongregationId == congregationId);

        if (parameters.TryGetValue("fromDate", out var from) && DateOnly.TryParse(from, out var fromDate))
            query = query.Where(ses => ses.ServiceElement.Service.Date >= fromDate);
        if (parameters.TryGetValue("toDate", out var to) && DateOnly.TryParse(to, out var toDate))
            query = query.Where(ses => ses.ServiceElement.Service.Date <= toDate);

        var results = await query
            .GroupBy(ses => new { ses.BundleId, ses.SongNumber, BundleName = ses.Bundle!.Abbreviation ?? ses.Bundle.Value })
            .Select(g => new { g.Key.BundleName, g.Key.SongNumber, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(20)
            .ToListAsync(ct);

        return new QueryResult
        {
            Title = "Meest gezongen liederen",
            ChartType = "bar",
            Columns = new() { "Lied", "Aantal" },
            Rows = results.Select(r => new Dictionary<string, object?>
            {
                ["Lied"] = $"{r.BundleName} {r.SongNumber}",
                ["Aantal"] = r.Count
            }).ToList(),
            TotalCount = results.Count,
            Chart = new ChartData
            {
                Labels = results.Select(r => $"{r.BundleName} {r.SongNumber}").ToList(),
                Datasets = new() { new() { Label = "Aantal keer gezongen", Data = results.Select(r => (double)r.Count).ToList() } }
            }
        };
    }

    private async Task<QueryResult> MostSungVerseAsync(Dictionary<string, string> parameters, CancellationToken ct)
    {
        var year = int.Parse(parameters.GetValueOrDefault("year", DateTime.Now.Year.ToString()));
        var query = _db.SongVerses
            .Include(sv => sv.ServiceElementSong)
                .ThenInclude(ses => ses.ServiceElement)
                    .ThenInclude(se => se.Service)
            .Include(sv => sv.ServiceElementSong)
                .ThenInclude(ses => ses.Bundle)
            .Where(sv => sv.ServiceElementSong.ServiceElement.Service.Date.Year == year);

        if (parameters.TryGetValue("bundleId", out var bundleIdStr) && Guid.TryParse(bundleIdStr, out var bundleId))
            query = query.Where(sv => sv.ServiceElementSong.BundleId == bundleId);

        var results = await query
            .GroupBy(sv => new
            {
                BundleName = sv.ServiceElementSong.Bundle!.Abbreviation ?? sv.ServiceElementSong.Bundle.Value,
                sv.ServiceElementSong.SongNumber,
                sv.VerseLabel
            })
            .Select(g => new { g.Key.BundleName, g.Key.SongNumber, g.Key.VerseLabel, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(20)
            .ToListAsync(ct);

        return new QueryResult
        {
            Title = $"Meest gezongen coupletten ({year})",
            ChartType = "bar",
            Columns = new() { "Couplet", "Aantal" },
            Rows = results.Select(r => new Dictionary<string, object?>
            {
                ["Couplet"] = $"{r.BundleName} {r.SongNumber}:{r.VerseLabel}",
                ["Aantal"] = r.Count
            }).ToList(),
            TotalCount = results.Count,
            Chart = new ChartData
            {
                Labels = results.Select(r => $"{r.BundleName} {r.SongNumber}:{r.VerseLabel}").ToList(),
                Datasets = new() { new() { Label = "Aantal", Data = results.Select(r => (double)r.Count).ToList() } }
            }
        };
    }

    private async Task<QueryResult> MostOpeningSongAsync(Dictionary<string, string> parameters, CancellationToken ct)
    {
        var query = _db.ServiceElementSongs
            .Include(ses => ses.ServiceElement)
                .ThenInclude(se => se.Service)
            .Include(ses => ses.Bundle)
            .Where(ses => ses.ServiceElement.Position <= 3); // Opening songs are typically first 3 elements

        if (parameters.TryGetValue("fromDate", out var from) && DateOnly.TryParse(from, out var fromDate))
            query = query.Where(ses => ses.ServiceElement.Service.Date >= fromDate);
        if (parameters.TryGetValue("toDate", out var to) && DateOnly.TryParse(to, out var toDate))
            query = query.Where(ses => ses.ServiceElement.Service.Date <= toDate);

        var results = await query
            .GroupBy(ses => new { ses.BundleId, ses.SongNumber, BundleName = ses.Bundle!.Abbreviation ?? ses.Bundle.Value })
            .Select(g => new { g.Key.BundleName, g.Key.SongNumber, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(15)
            .ToListAsync(ct);

        return new QueryResult
        {
            Title = "Meest gezongen openingsliederen",
            ChartType = "bar",
            Columns = new() { "Lied", "Aantal" },
            Rows = results.Select(r => new Dictionary<string, object?>
            {
                ["Lied"] = $"{r.BundleName} {r.SongNumber}",
                ["Aantal"] = r.Count
            }).ToList(),
            TotalCount = results.Count,
            Chart = new ChartData
            {
                Labels = results.Select(r => $"{r.BundleName} {r.SongNumber}").ToList(),
                Datasets = new() { new() { Label = "Aantal", Data = results.Select(r => (double)r.Count).ToList() } }
            }
        };
    }

    private async Task<QueryResult> AverageSongsPerServiceAsync(Dictionary<string, string> parameters, CancellationToken ct)
    {
        var query = _db.Services.AsQueryable();

        if (parameters.TryGetValue("congregationId", out var congIdStr) && Guid.TryParse(congIdStr, out var congId))
            query = query.Where(s => s.CongregationId == congId);
        if (parameters.TryGetValue("fromDate", out var from) && DateOnly.TryParse(from, out var fromDate))
            query = query.Where(s => s.Date >= fromDate);
        if (parameters.TryGetValue("toDate", out var to) && DateOnly.TryParse(to, out var toDate))
            query = query.Where(s => s.Date <= toDate);

        var results = await query
            .Include(s => s.Congregation)
            .Include(s => s.Elements)
                .ThenInclude(e => e.Songs)
                    .ThenInclude(ses => ses.Verses)
            .Select(s => new
            {
                Congregation = s.Congregation!.Name + " (" + s.Congregation.City + ")",
                SongCount = s.Elements.SelectMany(e => e.Songs).Count(),
                VerseCount = s.Elements.SelectMany(e => e.Songs).SelectMany(ses => ses.Verses).Count()
            })
            .ToListAsync(ct);

        var grouped = results
            .GroupBy(x => x.Congregation)
            .Select(g => new
            {
                Congregation = g.Key,
                AvgSongs = Math.Round(g.Average(x => x.SongCount), 1),
                AvgVerses = Math.Round(g.Average(x => x.VerseCount), 1)
            })
            .OrderByDescending(x => x.AvgVerses)
            .Take(20)
            .ToList();

        return new QueryResult
        {
            Title = "Gemiddeld aantal liederen/coupletten per dienst",
            ChartType = "bar",
            Columns = new() { "Gemeente", "Gem. liederen", "Gem. coupletten" },
            Rows = grouped.Select(r => new Dictionary<string, object?>
            {
                ["Gemeente"] = r.Congregation,
                ["Gem. liederen"] = r.AvgSongs,
                ["Gem. coupletten"] = r.AvgVerses
            }).ToList(),
            TotalCount = grouped.Count,
            Chart = new ChartData
            {
                Labels = grouped.Select(r => r.Congregation).ToList(),
                Datasets = new()
                {
                    new() { Label = "Gem. liederen", Data = grouped.Select(r => r.AvgSongs).ToList() },
                    new() { Label = "Gem. coupletten", Data = grouped.Select(r => r.AvgVerses).ToList() },
                }
            }
        };
    }

    private async Task<QueryResult> MostPsalmsCongregationAsync(Dictionary<string, string> parameters, CancellationToken ct)
    {
        // Find psalm bundles (Ps1773, PsOB, WKPs, etc.)
        var psalmBundles = await _db.ListItems
            .Where(li => li.Abbreviation != null && (li.Abbreviation.Contains("Ps") || li.Value.Contains("Psalm")))
            .Select(li => li.Id)
            .ToListAsync(ct);

        var query = _db.ServiceElementSongs
            .Include(ses => ses.ServiceElement)
                .ThenInclude(se => se.Service)
                    .ThenInclude(s => s!.Congregation)
            .AsQueryable();

        if (parameters.TryGetValue("fromDate", out var from) && DateOnly.TryParse(from, out var fromDate))
            query = query.Where(ses => ses.ServiceElement.Service.Date >= fromDate);
        if (parameters.TryGetValue("toDate", out var to) && DateOnly.TryParse(to, out var toDate))
            query = query.Where(ses => ses.ServiceElement.Service.Date <= toDate);

        var allSongs = await query
            .Select(ses => new
            {
                Congregation = ses.ServiceElement.Service.Congregation!.Name + " (" + ses.ServiceElement.Service.Congregation.City + ")",
                IsPsalm = psalmBundles.Contains(ses.BundleId)
            })
            .ToListAsync(ct);

        var grouped = allSongs
            .GroupBy(x => x.Congregation)
            .Select(g => new
            {
                Congregation = g.Key,
                Total = g.Count(),
                Psalms = g.Count(x => x.IsPsalm),
                Percentage = Math.Round(g.Count(x => x.IsPsalm) * 100.0 / g.Count(), 1)
            })
            .OrderByDescending(x => x.Percentage)
            .Take(20)
            .ToList();

        return new QueryResult
        {
            Title = "Gemeenten die het meeste psalmen zingen",
            ChartType = "bar",
            Columns = new() { "Gemeente", "Psalmen", "Totaal", "Percentage" },
            Rows = grouped.Select(r => new Dictionary<string, object?>
            {
                ["Gemeente"] = r.Congregation,
                ["Psalmen"] = r.Psalms,
                ["Totaal"] = r.Total,
                ["Percentage"] = $"{r.Percentage}%"
            }).ToList(),
            TotalCount = grouped.Count,
            Chart = new ChartData
            {
                Labels = grouped.Select(r => r.Congregation).ToList(),
                Datasets = new() { new() { Label = "% Psalmen", Data = grouped.Select(r => r.Percentage).ToList() } }
            }
        };
    }

    private async Task<QueryResult> SongByCityMapAsync(Dictionary<string, string> parameters, CancellationToken ct)
    {
        var bundleId = Guid.Parse(parameters["bundleId"]);
        var songNumber = int.Parse(parameters["songNumber"]);

        var query = _db.ServiceElementSongs
            .Include(ses => ses.ServiceElement)
                .ThenInclude(se => se.Service)
                    .ThenInclude(s => s!.Congregation)
            .Where(ses => ses.BundleId == bundleId && ses.SongNumber == songNumber);

        if (parameters.TryGetValue("fromDate", out var from) && DateOnly.TryParse(from, out var fromDate))
            query = query.Where(ses => ses.ServiceElement.Service.Date >= fromDate);
        if (parameters.TryGetValue("toDate", out var to) && DateOnly.TryParse(to, out var toDate))
            query = query.Where(ses => ses.ServiceElement.Service.Date <= toDate);

        var results = await query
            .GroupBy(ses => new
            {
                City = ses.ServiceElement.Service.Congregation!.City,
                Lat = ses.ServiceElement.Service.Congregation.Latitude,
                Lng = ses.ServiceElement.Service.Congregation.Longitude
            })
            .Select(g => new { g.Key.City, g.Key.Lat, g.Key.Lng, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync(ct);

        return new QueryResult
        {
            Title = $"Steden die dit lied het meest zingen",
            ChartType = "map",
            Columns = new() { "Stad", "Aantal", "Lat", "Lng" },
            Rows = results.Select(r => new Dictionary<string, object?>
            {
                ["Stad"] = r.City,
                ["Aantal"] = r.Count,
                ["Lat"] = r.Lat,
                ["Lng"] = r.Lng
            }).ToList(),
            TotalCount = results.Count,
        };
    }

    private async Task<QueryResult> SongByPeriodAsync(Dictionary<string, string> parameters, CancellationToken ct)
    {
        var year = int.Parse(parameters["year"]);
        var query = _db.ServiceElementSongs
            .Include(ses => ses.ServiceElement)
                .ThenInclude(se => se.Service)
            .Include(ses => ses.Bundle)
            .Where(ses => ses.ServiceElement.Service.Date.Year == year);

        if (parameters.TryGetValue("month", out var monthStr) && int.TryParse(monthStr, out var month))
            query = query.Where(ses => ses.ServiceElement.Service.Date.Month == month);

        var results = await query
            .GroupBy(ses => new { ses.BundleId, ses.SongNumber, BundleName = ses.Bundle!.Abbreviation ?? ses.Bundle.Value })
            .Select(g => new { g.Key.BundleName, g.Key.SongNumber, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(20)
            .ToListAsync(ct);

        return new QueryResult
        {
            Title = $"Meest gezongen liederen ({year})",
            ChartType = "bar",
            Columns = new() { "Lied", "Aantal" },
            Rows = results.Select(r => new Dictionary<string, object?>
            {
                ["Lied"] = $"{r.BundleName} {r.SongNumber}",
                ["Aantal"] = r.Count
            }).ToList(),
            TotalCount = results.Count,
            Chart = new ChartData
            {
                Labels = results.Select(r => $"{r.BundleName} {r.SongNumber}").ToList(),
                Datasets = new() { new() { Label = "Aantal", Data = results.Select(r => (double)r.Count).ToList() } }
            }
        };
    }

    private async Task<QueryResult> ServicesWithSongAsync(Dictionary<string, string> parameters, CancellationToken ct)
    {
        var bundleId = Guid.Parse(parameters["bundleId"]);
        var songNumber = int.Parse(parameters["songNumber"]);

        var results = await _db.ServiceElementSongs
            .Include(ses => ses.ServiceElement)
                .ThenInclude(se => se.Service)
                    .ThenInclude(s => s!.Congregation)
            .Include(ses => ses.ServiceElement)
                .ThenInclude(se => se.Service)
                    .ThenInclude(s => s!.Preacher)
            .Where(ses => ses.BundleId == bundleId && ses.SongNumber == songNumber)
            .Select(ses => new
            {
                Date = ses.ServiceElement.Service.Date,
                Congregation = ses.ServiceElement.Service.Congregation!.Name,
                City = ses.ServiceElement.Service.Congregation.City,
                Preacher = ses.ServiceElement.Service.Preacher!.FullName,
                Position = ses.ServiceElement.Position,
                Label = ses.ServiceElement.Label != null ? ses.ServiceElement.Label!.Value : ""
            })
            .OrderByDescending(x => x.Date)
            .Take(100)
            .ToListAsync(ct);

        return new QueryResult
        {
            Title = "Diensten met dit lied",
            ChartType = "table",
            Columns = new() { "Datum", "Gemeente", "Stad", "Voorganger", "Positie", "Label" },
            Rows = results.Select(r => new Dictionary<string, object?>
            {
                ["Datum"] = r.Date.ToString("d-M-yyyy"),
                ["Gemeente"] = r.Congregation,
                ["Stad"] = r.City,
                ["Voorganger"] = r.Preacher,
                ["Positie"] = r.Position,
                ["Label"] = r.Label
            }).ToList(),
            TotalCount = results.Count,
        };
    }

    private async Task<QueryResult> SongAfterSongAsync(Dictionary<string, string> parameters, CancellationToken ct)
    {
        var bundleIdA = Guid.Parse(parameters["bundleIdA"]);
        var songNumberA = int.Parse(parameters["songNumberA"]);
        var bundleIdB = Guid.Parse(parameters["bundleIdB"]);
        var songNumberB = int.Parse(parameters["songNumberB"]);

        // Find services where song B is followed by song A at the next position
        var servicesWithA = await _db.ServiceElementSongs
            .Include(ses => ses.ServiceElement)
                .ThenInclude(se => se.Service)
                    .ThenInclude(s => s!.Congregation)
            .Where(ses => ses.BundleId == bundleIdB && ses.SongNumber == songNumberB)
            .Select(ses => new { ses.ServiceElement.ServiceId, PositionB = ses.ServiceElement.Position })
            .ToListAsync(ct);

        var serviceIds = servicesWithA.Select(x => x.ServiceId).Distinct().ToList();

        var matchingAfter = await _db.ServiceElementSongs
            .Include(ses => ses.ServiceElement)
                .ThenInclude(se => se.Service)
                    .ThenInclude(s => s!.Congregation)
            .Where(ses => ses.BundleId == bundleIdA && ses.SongNumber == songNumberA
                && serviceIds.Contains(ses.ServiceElement.ServiceId))
            .Select(ses => new
            {
                ses.ServiceElement.ServiceId,
                PositionA = ses.ServiceElement.Position,
                Date = ses.ServiceElement.Service.Date,
                Congregation = ses.ServiceElement.Service.Congregation!.Name,
                City = ses.ServiceElement.Service.Congregation.City
            })
            .ToListAsync(ct);

        // Match where A directly follows B
        var results = (from a in matchingAfter
                       join b in servicesWithA on a.ServiceId equals b.ServiceId
                       where a.PositionA == b.PositionB + 1
                       select a).ToList();

        return new QueryResult
        {
            Title = "Lied direct na ander lied",
            ChartType = "table",
            Columns = new() { "Datum", "Gemeente", "Stad" },
            Rows = results.Select(r => new Dictionary<string, object?>
            {
                ["Datum"] = r.Date.ToString("d-M-yyyy"),
                ["Gemeente"] = r.Congregation,
                ["Stad"] = r.City
            }).ToList(),
            TotalCount = results.Count,
        };
    }

    private async Task<QueryResult> SongUsageOverTimeAsync(Dictionary<string, string> parameters, CancellationToken ct)
    {
        var bundleId = Guid.Parse(parameters["bundleId"]);
        var songNumber = int.Parse(parameters["songNumber"]);

        var results = await _db.ServiceElementSongs
            .Include(ses => ses.ServiceElement)
                .ThenInclude(se => se.Service)
            .Where(ses => ses.BundleId == bundleId && ses.SongNumber == songNumber)
            .GroupBy(ses => ses.ServiceElement.Service.Date.Year)
            .Select(g => new { Year = g.Key, Count = g.Count() })
            .OrderBy(x => x.Year)
            .ToListAsync(ct);

        return new QueryResult
        {
            Title = "Gebruik over tijd",
            ChartType = "line",
            Columns = new() { "Jaar", "Aantal" },
            Rows = results.Select(r => new Dictionary<string, object?>
            {
                ["Jaar"] = r.Year,
                ["Aantal"] = r.Count
            }).ToList(),
            TotalCount = results.Count,
            Chart = new ChartData
            {
                Labels = results.Select(r => r.Year.ToString()).ToList(),
                Datasets = new() { new() { Label = "Aantal keer gezongen", Data = results.Select(r => (double)r.Count).ToList() } }
            }
        };
    }
}
