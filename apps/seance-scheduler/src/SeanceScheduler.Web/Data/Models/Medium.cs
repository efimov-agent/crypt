namespace SeanceScheduler.Web.Data.Models;

/// <summary>
/// A spiritualist medium who can conduct seances
/// </summary>
public class Medium
{
    public int Id { get; set; }

    public required string Name { get; set; }

    /// <summary>Upper-cased (invariant) name: enforces case-insensitive uniqueness for any script, not only ASCII.</summary>
    public required string NormalizedName { get; set; }

    public static string Normalize(string name) => name.Trim().ToUpperInvariant();

    public string? Specialty { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Seance> Seances { get; set; } = new List<Seance>();
}