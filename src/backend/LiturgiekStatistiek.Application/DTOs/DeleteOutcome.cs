namespace LiturgiekStatistiek.Application.DTOs;

/// <summary>Result of attempting to delete a curated reference entity.</summary>
public enum DeleteOutcome
{
    Deleted = 0,
    NotFound = 1,
    HasReferences = 2
}
