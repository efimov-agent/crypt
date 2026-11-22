namespace SeanceScheduler.Web.Features.Common;

/// <summary>
/// RFC 9457 problem responses (application/problem+json) for API errors.
/// </summary>
public static class ApiProblems
{
    public static IResult NotFound(string detail, string instance) =>
        Results.Problem(detail: detail, instance: instance, statusCode: StatusCodes.Status404NotFound, title: "Not Found");

    public static IResult Conflict(string detail, string instance) =>
        Results.Problem(detail: detail, instance: instance, statusCode: StatusCodes.Status409Conflict, title: "Conflict");
}
