namespace SeanceScheduler.Web.Features.Common;

/// <summary>
/// Thrown when a business rule conflict occurs (duplicate, inactive, overlap, etc.)
/// </summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }
}