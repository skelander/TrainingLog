using TrainingLog.Data;

namespace TrainingLog.Services;

public class AuthService(AppDbContext db) : IAuthService
{
    public string? Authenticate(string username, string password)
    {
        var user = db.Users.FirstOrDefault(u => u.Username == username && u.Password == password);
        return user?.Role;
    }
}
