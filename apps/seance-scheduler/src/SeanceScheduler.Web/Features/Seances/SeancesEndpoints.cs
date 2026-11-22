using Microsoft.AspNetCore.Mvc;
using SeanceScheduler.Web.Features.Common;

namespace SeanceScheduler.Web.Features.Seances;

public static class SeancesEndpoints
{
    public static void MapSeancesEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/seances");

        group.MapGet("/", GetSeances);
        group.MapPost("/", CreateSeance);
        group.MapPost("/{id:int}/cancel", CancelSeance);
    }

    private static async Task<IResult> GetSeances(
        [FromQuery] int? mediumId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        SeancesService service,
        ILogger<SeancesService> logger,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("GET /api/seances requested. Filters: mediumId={MediumId}, from={From}, to={To}",
            mediumId, from, to);

        try
        {
            var seances = await service.GetSeancesAsync(mediumId, from, to, cancellationToken);
            logger.LogInformation("Returning {Count} seances", seances.Count);
            return Results.Ok(seances);
        }
        catch (ValidationException ex)
        {
            logger.LogWarning("Invalid query parameters: {Errors}", string.Join(", ", ex.Errors.SelectMany(kv => kv.Value)));
            return Results.ValidationProblem(ex.Errors);
        }
    }

    private static async Task<IResult> CreateSeance(CreateSeanceRequest request, SeancesService service, ILogger<SeancesService> logger, CancellationToken cancellationToken)
    {
        var errors = request.Validate();
        if (errors.Count > 0)
        {
            logger.LogWarning("Invalid seance request: {Errors}", string.Join(", ", errors.SelectMany(kv => kv.Value)));
            return Results.ValidationProblem(errors);
        }

        try
        {
            var created = await service.BookSeanceAsync(
                request.MediumId,
                request.ClientName!.Trim(),
                request.StartsAt,
                request.EndsAt,
                cancellationToken);

            logger.LogInformation("Created seance {Id}", created.Id);
            return Results.Created($"/api/seances/{created.Id}", created);
        }
        catch (NotFoundException ex)
        {
            logger.LogWarning("Not found creating seance: {Message}", ex.Message);
            return Results.NotFound(new ProblemDetails
            {
                Type = "https://example.com/errors/not-found",
                Title = "Not Found",
                Status = 404,
                Detail = ex.Message,
                Instance = "/api/seances"
            });
        }
        catch (ConflictException ex)
        {
            logger.LogWarning("Conflict creating seance: {Message}", ex.Message);
            return Results.Conflict(new ProblemDetails
            {
                Type = "https://example.com/errors/conflict",
                Title = "Conflict",
                Status = 409,
                Detail = ex.Message,
                Instance = "/api/seances"
            });
        }
        catch (ValidationException ex)
        {
            logger.LogWarning("Validation error creating seance: {Errors}", string.Join(", ", ex.Errors.SelectMany(kv => kv.Value)));
            return Results.ValidationProblem(ex.Errors);
        }
    }

    private static async Task<IResult> CancelSeance(int id, SeancesService service, ILogger<SeancesService> logger, CancellationToken cancellationToken)
    {
        logger.LogInformation("POST /api/seances/{Id}/cancel requested", id);

        try
        {
            var cancelled = await service.CancelSeanceAsync(id, cancellationToken);
            logger.LogInformation("Cancelled seance {Id}", id);
            return Results.Ok(cancelled);
        }
        catch (NotFoundException ex)
        {
            logger.LogWarning("Seance {Id} not found for cancellation: {Message}", id, ex.Message);
            return Results.NotFound(new ProblemDetails
            {
                Type = "https://example.com/errors/not-found",
                Title = "Not Found",
                Status = 404,
                Detail = ex.Message,
                Instance = $"/api/seances/{id}/cancel"
            });
        }
        catch (ConflictException ex)
        {
            logger.LogWarning("Conflict cancelling seance {Id}: {Message}", id, ex.Message);
            return Results.Conflict(new ProblemDetails
            {
                Type = "https://example.com/errors/conflict",
                Title = "Conflict",
                Status = 409,
                Detail = ex.Message,
                Instance = $"/api/seances/{id}/cancel"
            });
        }
    }
}