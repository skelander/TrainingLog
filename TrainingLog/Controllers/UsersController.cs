using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TrainingLog.Services;

namespace TrainingLog.Controllers;

[ApiController]
[Route("users")]
[Authorize(Roles = "admin")]
public class UsersController(IUsersService service, ILogger<UsersController> logger) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        Ok(await service.GetAllAsync(cancellationToken));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var user = await service.GetByIdAsync(id, cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Length > 50)
            return BadRequest(new { error = "Username must be 1–50 characters." });
        if (string.IsNullOrEmpty(request.Password))
            return BadRequest(new { error = "Password is required." });
        if (request.Password.Length > 72)
            return BadRequest(new { error = "Password must be at most 72 characters." });
        if (request.Role is not "user" and not "admin")
            return BadRequest(new { error = "Role must be 'user' or 'admin'." });

        try
        {
            var user = await service.CreateAsync(request, cancellationToken);
            logger.LogInformation("Admin created user {UserId} ({Username})", user.Id, user.Username);
            return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
        }
        catch (DomainException ex)
        {
            logger.LogWarning("Create user failed: {Reason}", ex.Message);
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Length > 50)
            return BadRequest(new { error = "Username must be 1–50 characters." });
        if (request.Role is not "user" and not "admin")
            return BadRequest(new { error = "Role must be 'user' or 'admin'." });
        if (!string.IsNullOrEmpty(request.Password) && request.Password.Length > 72)
            return BadRequest(new { error = "Password must be at most 72 characters." });

        try
        {
            var user = await service.UpdateAsync(id, request, cancellationToken);
            if (user is null) return NotFound();
            logger.LogInformation("Admin updated user {UserId}", id);
            return Ok(user);
        }
        catch (DomainException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        if (id == CurrentUserId)
            return BadRequest(new { error = "Cannot delete your own account." });

        var result = await service.DeleteAsync(id, cancellationToken);
        if (result)
            logger.LogInformation("Admin deleted user {UserId}", id);
        return result ? NoContent() : NotFound();
    }
}
