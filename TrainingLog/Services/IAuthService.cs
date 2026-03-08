namespace TrainingLog.Services;

public record LoginResult(string Token, string Role);

public interface IAuthService
{
    Task<LoginResult?> LoginAsync(string username, string password, CancellationToken cancellationToken = default);
}
