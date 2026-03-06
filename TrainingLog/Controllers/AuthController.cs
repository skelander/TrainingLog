using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using TrainingLog.Services;

namespace TrainingLog.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController(IAuthService auth, IConfiguration config) : ControllerBase
{
    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            return BadRequest("Username and password are required.");

        var result = auth.Authenticate(request.Username, request.Password);
        if (result is null) return Unauthorized();
        var (userId, role) = result.Value;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, request.Username),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Role, role),
        };
        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds);

        return Ok(new LoginResponse(request.Username, role, new JwtSecurityTokenHandler().WriteToken(token)));
    }
}

public record LoginRequest(string Username, string Password);
public record LoginResponse(string User, string Role, string Token);
