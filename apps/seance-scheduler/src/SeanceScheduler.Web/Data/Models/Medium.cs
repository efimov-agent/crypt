namespace SeanceScheduler.Web.Data.Models;

/// <summary>
/// A spiritualist medium who can conduct seances
/// </summary>
public class Medium
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public string? Specialty { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Seance> Seances { get; set; } = new List<Seance>();
}