namespace SeanceScheduler.Web.Features.Common;

/// <summary>
/// Thrown by services when request values fail domain validation (mapped to 400 validation problem)
/// </summary>
public class RequestValidationException : Exception
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public RequestValidationException(Dictionary<string, string[]> errors)
        : base("Validation failed")
    {
        Errors = errors;
    }
}