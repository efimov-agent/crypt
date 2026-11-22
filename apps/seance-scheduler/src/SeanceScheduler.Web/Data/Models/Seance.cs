namespace SeanceScheduler.Web.Data.Models;

/// <summary>
/// A scheduled seance appointment with a medium
/// </summary>
public class Seance
{
    public int Id { get; set; }

    public int MediumId { get; set; }

    public required string ClientName { get; set; }

    public DateTime StartsAt { get; set; }

    public DateTime EndsAt { get; set; }

    public string Status { get; set; } = "Scheduled";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CancelledAt { get; set; }

    public Medium? Medium { get; set; }
}