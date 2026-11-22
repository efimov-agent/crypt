namespace SeanceScheduler.Web.Features.Mediums;

/// <summary>
/// DTO for returning Medium data
/// </summary>
public sealed record MediumDto(int Id, string Name, string? Specialty, bool IsActive, DateTime CreatedAt);