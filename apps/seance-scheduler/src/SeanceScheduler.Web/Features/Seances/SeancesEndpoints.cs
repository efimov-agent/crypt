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
        try
        {
            var seances = await service.GetSeancesAsync(mediumId, from, to, cancellationToken);
            return Results.Ok(seances);
        }
        catch (RequestValidationException ex)
        {
            logger.LogWarning("Invalid query parameters: {Errors}", string.Join(", ", ex.Errors.SelectMany(kv => kv.Value)));
            return Results.ValidationProblem(ex.Errors);
        }
    }

    private static async Task<IResult> CreateSeance(CreateSeanceRequest request, SeancesService service, TimeProvider timeProvider, ILogger<SeancesService> logger, CancellationToken cancellationToken)
    {
        var errors = request.Validate(timeProvider.GetUtcNow().UtcDateTime);
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

            return Results.Created($"/api/seances/{created.Id}", created);
        }
        catch (NotFoundException ex)
        {
            return ApiProblems.NotFound(ex.Message, "/api/seances");
        }
        catch (ConflictException ex)
        {
            return ApiProblems.Conflict(ex.Message, "/api/seances");
        }
        catch (RequestValidationException ex)
        {
            logger.LogWarning("Validation error creating seance: {Errors}", string.Join(", ", ex.Errors.SelectMany(kv => kv.Value)));
            return Results.ValidationProblem(ex.Errors);
        }
    }

    private static async Task<IResult> CancelSeance(int id, SeancesService service, CancellationToken cancellationToken)
    {
        try
        {
            var cancelled = await service.CancelSeanceAsync(id, cancellationToken);
            return Results.Ok(cancelled);
        }
        catch (NotFoundException ex)
        {
            return ApiProblems.NotFound(ex.Message, $"/api/seances/{id}/cancel");
        }
        catch (ConflictException ex)
        {
            return ApiProblems.Conflict(ex.Message, $"/api/seances/{id}/cancel");
        }
    }
}