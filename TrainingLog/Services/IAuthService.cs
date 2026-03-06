namespace TrainingLog.Services;

public interface IAuthService
{
    (int UserId, string Role)? Authenticate(string username, string password);
}
