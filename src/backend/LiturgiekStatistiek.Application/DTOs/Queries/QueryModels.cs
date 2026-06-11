namespace LiturgiekStatistiek.Application.DTOs.Queries;

public record QueryRequest
{
    public string? TemplateId { get; init; }
    public string? NaturalLanguageQuery { get; init; }
    public Dictionary<string, string>? Parameters { get; init; }
}

public record QueryResult
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string ChartType { get; init; } = "table";
    public List<string> Columns { get; init; } = new();
    public List<Dictionary<string, object?>> Rows { get; init; } = new();
    public int TotalCount { get; init; }
    public ChartData? Chart { get; init; }
}

public record ChartData
{
    public List<string> Labels { get; init; } = new();
    public List<ChartDataset> Datasets { get; init; } = new();
}

public record ChartDataset
{
    public string Label { get; init; } = string.Empty;
    public List<double> Data { get; init; } = new();
    public string? BackgroundColor { get; init; }
}

public record QueryTemplate
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public List<QueryParameter> Parameters { get; init; } = new();
    public string DefaultChartType { get; init; } = "bar";
}

public record QueryParameter
{
    public string Name { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string Type { get; init; } = "string";
    public bool Required { get; init; }
    public string? DefaultValue { get; init; }
}
