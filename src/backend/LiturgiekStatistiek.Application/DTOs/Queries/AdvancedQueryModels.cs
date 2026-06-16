namespace LiturgiekStatistiek.Application.DTOs.Queries;

/// <summary>
/// A user-built advanced query over Services and related data. All filters are
/// AND-combined. Output is either a paginated list of matching services or a
/// group-by aggregate count.
/// </summary>
public record AdvancedQueryDefinition
{
    /// <summary>Optional display name (used for multi-query comparison).</summary>
    public string? Name { get; init; }

    public List<AdvancedFilter> Filters { get; init; } = new();

    /// <summary>"list" (default) or "aggregate".</summary>
    public string OutputMode { get; init; } = "list";

    /// <summary>Group-by field key, required when OutputMode == "aggregate".</summary>
    public string? GroupBy { get; init; }

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;

    /// <summary>Preferred chart type for aggregate output (bar/line/pie/doughnut).</summary>
    public string ChartType { get; init; } = "bar";
}

/// <summary>
/// A single AND-ed filter block. <see cref="Field"/> and <see cref="Operator"/>
/// are whitelisted server-side; nothing here is used to build raw SQL.
/// </summary>
public record AdvancedFilter
{
    public string Field { get; init; } = string.Empty;
    public string Operator { get; init; } = "eq";

    /// <summary>Primary value (Guid/string/date/number/bool depending on field).</summary>
    public string? Value { get; init; }

    /// <summary>Secondary value, used for "between" (date range upper bound).</summary>
    public string? Value2 { get; init; }

    // --- Song-sequence operands (Field == "songSequence") ---
    public string? SongBundleA { get; init; }
    public int? SongNumberA { get; init; }
    public string? SongBundleB { get; init; }
    public int? SongNumberB { get; init; }
}

/// <summary>Request body for the multi-query comparison endpoint.</summary>
public record CompareQueriesRequest
{
    public List<AdvancedQueryDefinition> Queries { get; init; } = new();
}

/// <summary>Describes one whitelisted field that the builder UI can offer.</summary>
public record AdvancedField
{
    public string Key { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    /// <summary>Input type hint for the UI: text/date/number/bool/congregation/preacher/list/timeOfDay/song/songSequence.</summary>
    public string Type { get; init; } = "text";
    public List<string> Operators { get; init; } = new();
    /// <summary>For list-backed fields, the ListDefinition key to populate options.</summary>
    public string? ListKey { get; init; }
    public bool CanGroupBy { get; init; }
}

/// <summary>Schema returned to the builder UI: available fields + group-by options.</summary>
public record AdvancedQuerySchema
{
    public List<AdvancedField> Fields { get; init; } = new();
    public List<AdvancedField> GroupByFields { get; init; } = new();
}
