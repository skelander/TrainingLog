using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TrainingLog.Services;

namespace TrainingLog.Controllers;

[ApiController]
[Route("workout-types")]
[Authorize]
public class WorkoutTypesController(IWorkoutTypesService service, ILogger<WorkoutTypesController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        Ok(await service.GetAllAsync(cancellationToken));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var type = await service.GetByIdAsync(id, cancellationToken);
        return type is null ? NotFound() : Ok(type);
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Create([FromBody] WorkoutTypeRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 100)
            return BadRequest("Name must be 1–100 characters.");
        if (request.Fields == null)
            return BadRequest("Fields is required.");

        var type = await service.CreateAsync(request.Name, request.Fields, cancellationToken);
        logger.LogInformation("Workout type {TypeId} ({Name}) created", type.Id, type.Name);
        return CreatedAtAction(nameof(GetById), new { id = type.Id }, type);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Update(int id, [FromBody] WorkoutTypeRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 100)
            return BadRequest("Name must be 1–100 characters.");
        if (request.Fields == null)
            return BadRequest("Fields is required.");

        try
        {
            var type = await service.UpdateAsync(id, request.Name, request.Fields, cancellationToken);
            if (type is null) return NotFound();
            logger.LogInformation("Workout type {TypeId} updated", id);
            return Ok(type);
        }
        catch (DomainException ex)
        {
            logger.LogWarning("Cannot update workout type {TypeId}: {Reason}", id, ex.Message);
            return Conflict(new { error = ex.Message });
        }
        catch (DbUpdateException)
        {
            return Conflict(new { error = "The resource was modified concurrently. Please retry." });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await service.DeleteAsync(id, cancellationToken);
        if (result is true)
            logger.LogInformation("Workout type {TypeId} deleted", id);
        else if (result is false)
            logger.LogWarning("Cannot delete workout type {TypeId}: has existing sessions", id);
        return result switch
        {
            true => NoContent(),
            false => Conflict(new { error = "Cannot delete workout type with existing sessions." }),
            null => NotFound(),
        };
    }
}

public record WorkoutTypeRequest(string Name, List<TrainingLog.Services.FieldDefinitionRequest> Fields);
