using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TrainingLog.Services;

namespace TrainingLog.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController(IAuthService auth, ILogger<AuthController> logger) : ControllerBase
{
    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            return BadRequest(new { error = "Username and password are required." });
        if (request.Password.Length > Limits.PasswordMaxLength)
            return BadRequest(new { error = $"Password must be at most {Limits.PasswordMaxLength} characters." });

        var result = await auth.LoginAsync(request.Username, request.Password, cancellationToken);
        if (result is null)
        {
            logger.LogWarning("Failed login attempt for user {Username}", request.Username);
            return Unauthorized();
        }
        logger.LogInformation("User {Username} logged in with role {Role}", request.Username, result.Role);
        return Ok(new LoginResponse(request.Username, result.Role, result.Token));
    }
}

public record LoginRequest(string Username, string Password);
public record LoginResponse(string User, string Role, string Token);
