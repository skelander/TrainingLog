using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainingLog.Services;

namespace TrainingLog.Controllers;

[ApiController]
[Route("workouts")]
[Authorize]
[ValidateUserId]
public class WorkoutsController(IWorkoutsService service, ILogger<WorkoutsController> logger) : ControllerBase
{
    private int CurrentUserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
    private bool IsAdmin => User.IsInRole("admin");

    [HttpGet]
    public async Task<IActionResult> GetMine(CancellationToken cancellationToken) =>
        Ok(await service.GetForUserAsync(CurrentUserId, cancellationToken));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var session = await service.GetByIdAsync(id, cancellationToken);
        if (session is null || (!IsAdmin && session.UserId != CurrentUserId)) return NotFound();
        return Ok(session);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWorkoutRequest request, CancellationToken cancellationToken)
    {
        if (request.WorkoutTypeId <= 0)
            return BadRequest("WorkoutTypeId must be a positive integer.");
        if (request.Values == null)
            return BadRequest("Values is required.");
        if (request.Notes?.Length > 1000)
            return BadRequest("Notes must be at most 1000 characters.");
        if (request.Values.Any(v => v.Value.Length > 500))
            return BadRequest("Field values must be at most 500 characters.");

        try
        {
            var session = await service.CreateAsync(CurrentUserId, request.WorkoutTypeId, request.LoggedAt, request.Notes, request.Values, cancellationToken);
            if (session is null) return BadRequest("Workout type not found.");
            logger.LogInformation("User {UserId} logged workout session {SessionId}", CurrentUserId, session.Id);
            return CreatedAtAction(nameof(GetById), new { id = session.Id }, session);
        }
        catch (DomainException ex)
        {
            logger.LogWarning("Create workout failed for user {UserId}: {Reason}", CurrentUserId, ex.Message);
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await service.DeleteAsync(id, CurrentUserId, IsAdmin, cancellationToken);
        if (result is true)
            logger.LogInformation("User {UserId} deleted workout session {SessionId}", CurrentUserId, id);
        return result switch
        {
            true => NoContent(),
            false => NotFound(),
            null => NotFound(),
        };
    }
}

public record CreateWorkoutRequest(
    int WorkoutTypeId,
    DateTime LoggedAt,
    string? Notes,
    List<TrainingLog.Services.FieldValueRequest> Values);
