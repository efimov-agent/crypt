using Microsoft.AspNetCore.Mvc;
using SeanceScheduler.Web.Features.Common;

namespace SeanceScheduler.Web.Features.Mediums;

public static class MediumsEndpoints
{
    public static void MapMediumsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/mediums");

        group.MapGet("/", GetMediums);
        group.MapGet("/{id:int}", GetMedium);
        group.MapPost("/", CreateMedium);
        group.MapPost("/{id:int}/deactivate", DeactivateMedium);
    }

    private static async Task<IResult> GetMediums(MediumsService service, ILogger<MediumsService> logger, CancellationToken cancellationToken)
    {
        logger.LogInformation("GET /api/mediums requested");

        var mediums = await service.GetMediumsAsync(cancellationToken);

        logger.LogInformation("Returning {Count} mediums", mediums.Count);
        return Results.Ok(mediums);
    }

    private static async Task<IResult> GetMedium(int id, MediumsService service, ILogger<MediumsService> logger, CancellationToken cancellationToken)
    {
        logger.LogInformation("GET /api/mediums/{Id} requested", id);

        var medium = await service.GetMediumByIdAsync(id, cancellationToken);

        if (medium == null)
        {
            logger.LogWarning("Medium {Id} not found", id);
            return Results.NotFound(new ProblemDetails
            {
                Type = "https://example.com/errors/not-found",
                Title = "Not Found",
                Status = 404,
                Detail = $"Medium {id} not found",
                Instance = $"/api/mediums/{id}"
            });
        }

        return Results.Ok(medium);
    }

    private static async Task<IResult> CreateMedium(CreateMediumRequest request, MediumsService service, ILogger<MediumsService> logger, CancellationToken cancellationToken)
    {
        var errors = request.Validate();
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        try
        {
            var created = await service.CreateMediumAsync(
                request.Name!.Trim(),
                string.IsNullOrWhiteSpace(request.Specialty) ? null : request.Specialty.Trim(),
                cancellationToken);

            logger.LogInformation("Created medium {Id}", created.Id);
            return Results.Created($"/api/mediums/{created.Id}", created);
        }
        catch (ConflictException ex)
        {
            logger.LogWarning("Conflict creating medium: {Message}", ex.Message);
            return Results.Conflict(new ProblemDetails
            {
                Type = "https://example.com/errors/conflict",
                Title = "Conflict",
                Status = 409,
                Detail = ex.Message,
                Instance = "/api/mediums"
            });
        }
    }

    private static async Task<IResult> DeactivateMedium(int id, MediumsService service, ILogger<MediumsService> logger, CancellationToken cancellationToken)
    {
        logger.LogInformation("POST /api/mediums/{Id}/deactivate requested", id);

        try
        {
            var deactivated = await service.DeactivateMediumAsync(id, cancellationToken);
            logger.LogInformation("Deactivated medium {Id}", id);
            return Results.Ok(deactivated);
        }
        catch (NotFoundException ex)
        {
            logger.LogWarning("Medium {Id} not found for deactivation: {Message}", id, ex.Message);
            return Results.NotFound(new ProblemDetails
            {
                Type = "https://example.com/errors/not-found",
                Title = "Not Found",
                Status = 404,
                Detail = ex.Message,
                Instance = $"/api/mediums/{id}/deactivate"
            });
        }
    }
}