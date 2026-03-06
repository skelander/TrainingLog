using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainingLog.Services;

namespace TrainingLog.Controllers;

[ApiController]
[Route("workouts")]
[Authorize]
public class WorkoutsController(IWorkoutsService service) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool IsAdmin => User.IsInRole("admin");

    [HttpGet]
    public IActionResult GetMine() => Ok(service.GetForUser(CurrentUserId));

    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        var session = service.GetById(id);
        if (session is null) return NotFound();
        if (!IsAdmin && session.UserId != CurrentUserId) return Forbid();
        return Ok(session);
    }

    [HttpPost]
    public IActionResult Create([FromBody] CreateWorkoutRequest request)
    {
        if (request.WorkoutTypeId <= 0)
            return BadRequest("WorkoutTypeId must be a positive integer.");
        if (request.Values == null)
            return BadRequest("Values is required.");
        if (request.Notes?.Length > 1000)
            return BadRequest("Notes must be at most 1000 characters.");

        var session = service.Create(CurrentUserId, request.WorkoutTypeId, request.LoggedAt, request.Notes, request.Values);
        if (session is null) return BadRequest("Workout type not found.");
        return CreatedAtAction(nameof(GetById), new { id = session.Id }, session);
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        var deleted = service.Delete(id, CurrentUserId, IsAdmin);
        if (!deleted)
        {
            var session = service.GetById(id);
            if (session is null) return NotFound();
            return Forbid();
        }
        return NoContent();
    }
}

public record CreateWorkoutRequest(
    int WorkoutTypeId,
    DateTime LoggedAt,
    string? Notes,
    List<TrainingLog.Services.FieldValueRequest> Values);
