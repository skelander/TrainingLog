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
    private string CurrentUser => User.FindFirstValue(ClaimTypes.Name)!;
    private bool IsAdmin => User.IsInRole("admin");

    [HttpGet]
    public IActionResult GetMine() => Ok(service.GetForUser(CurrentUser));

    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        var session = service.GetById(id);
        if (session is null) return NotFound();
        if (!IsAdmin && session.Username != CurrentUser) return Forbid();
        return Ok(session);
    }

    [HttpPost]
    public IActionResult Create([FromBody] CreateWorkoutRequest request)
    {
        var session = service.Create(CurrentUser, request.WorkoutTypeId, request.LoggedAt, request.Notes, request.Values);
        if (session is null) return BadRequest("Workout type not found.");
        return CreatedAtAction(nameof(GetById), new { id = session.Id }, session);
    }

    [HttpDelete("{id}")]
    public IActionResult Delete(int id)
    {
        var deleted = service.Delete(id, CurrentUser, IsAdmin);
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
