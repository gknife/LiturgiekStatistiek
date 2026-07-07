namespace LiturgiekStatistiek.Domain.Entities;

/// <summary>
/// Lifecycle state of a service. Concept (draft) services are excluded from
/// research queries, statistics and exports until published.
/// </summary>
public enum ServiceStatus
{
    Concept = 0,
    Gepubliceerd = 1
}
