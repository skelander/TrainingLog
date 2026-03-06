namespace TrainingLog.Services;

public interface IAuthService
{
    Task<(int UserId, string Role)?> AuthenticateAsync(string username, string password);
}
