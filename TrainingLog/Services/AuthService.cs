using Microsoft.EntityFrameworkCore;
using TrainingLog.Data;

namespace TrainingLog.Services;

public class AuthService(AppDbContext db) : IAuthService
{
    public async Task<(int UserId, string Role)?> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
        if (user is null) return null;
        return BCrypt.Net.BCrypt.Verify(password, user.Password) ? (user.Id, user.Role) : null;
    }
}
