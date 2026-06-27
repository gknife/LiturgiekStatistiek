namespace LiturgiekStatistiek.Application.DTOs;

public record RecentSearchDto(Guid Id, string QueryText, DateTime CreatedAt);

public record AddRecentSearchRequest(string QueryText);
