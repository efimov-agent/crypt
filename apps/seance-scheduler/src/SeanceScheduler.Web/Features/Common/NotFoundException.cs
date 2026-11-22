namespace SeanceScheduler.Web.Features.Common;

/// <summary>
/// Thrown when a requested resource is not found
/// </summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }
}