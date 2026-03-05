using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainingLog.Services;

namespace TrainingLog.Controllers;

[ApiController]
[Route("workout-types")]
public class WorkoutTypesController(IWorkoutTypesService service) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public IActionResult GetAll() => Ok(service.GetAll());

    [HttpGet("{id}")]
    [Authorize]
    public IActionResult GetById(int id)
    {
        var type = service.GetById(id);
        return type is null ? NotFound() : Ok(type);
    }

    [HttpPost]
    [Authorize(Roles = "admin")]
    public IActionResult Create([FromBody] WorkoutTypeRequest request)
    {
        var type = service.Create(request.Name, request.Fields);
        return CreatedAtAction(nameof(GetById), new { id = type.Id }, type);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "admin")]
    public IActionResult Update(int id, [FromBody] WorkoutTypeRequest request)
    {
        var type = service.Update(id, request.Name, request.Fields);
        return type is null ? NotFound() : Ok(type);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "admin")]
    public IActionResult Delete(int id)
    {
        return service.Delete(id) ? NoContent() : NotFound();
    }
}

public record WorkoutTypeRequest(string Name, List<TrainingLog.Services.FieldDefinitionRequest> Fields);
