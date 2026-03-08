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
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
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
            return BadRequest(new { error = "WorkoutTypeId must be a positive integer." });
        if (request.Values == null)
            return BadRequest(new { error = "Values is required." });
        if (request.Notes?.Length > 1000)
            return BadRequest(new { error = "Notes must be at most 1000 characters." });
        if (request.Values.Any(v => v.Value is null || v.Value.Length > 500))
            return BadRequest(new { error = "Field values must be at most 500 characters." });

        try
        {
            var session = await service.CreateAsync(new CreateSessionRequest(CurrentUserId, request.WorkoutTypeId, request.LoggedAt, request.Notes, request.Values), cancellationToken);
            if (session is null) return BadRequest(new { error = "Workout type not found." });
            logger.LogInformation("User {UserId} logged workout session {SessionId}", CurrentUserId, session.Id);
            return CreatedAtAction(nameof(GetById), new { id = session.Id }, session);
        }
        catch (DomainException ex)
        {
            logger.LogWarning("Create workout failed for user {UserId}: {Reason}", CurrentUserId, ex.Message);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateWorkoutRequest request, CancellationToken cancellationToken)
    {
        if (request.Values == null)
            return BadRequest(new { error = "Values is required." });
        if (request.Notes?.Length > 1000)
            return BadRequest(new { error = "Notes must be at most 1000 characters." });
        if (request.Values.Any(v => v.Value is null || v.Value.Length > 500))
            return BadRequest(new { error = "Field values must be at most 500 characters." });

        try
        {
            var session = await service.UpdateAsync(id, CurrentUserId, IsAdmin,
                new UpdateSessionRequest(request.LoggedAt, request.Notes, request.Values), cancellationToken);
            if (session is null) return NotFound();
            logger.LogInformation("User {UserId} updated workout session {SessionId}", CurrentUserId, id);
            return Ok(session);
        }
        catch (DomainException ex)
        {
            logger.LogWarning("Update workout failed for user {UserId}: {Reason}", CurrentUserId, ex.Message);
            return BadRequest(new { error = ex.Message });
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
    DateTimeOffset LoggedAt,
    string? Notes,
    List<TrainingLog.Services.FieldValueRequest> Values);

public record UpdateWorkoutRequest(
    DateTimeOffset LoggedAt,
    string? Notes,
    List<TrainingLog.Services.FieldValueRequest> Values);
