using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TrainingLog.Data;
using TrainingLog.Models;

namespace TrainingLog.Services;

public class UsersService(AppDbContext db, IConfiguration config) : IUsersService
{
    private string Hash(string pw) =>
        BCrypt.Net.BCrypt.HashPassword(pw, config.GetValue<int>("BCrypt:WorkFactor", 11));

    public async Task<List<UserResponse>> GetAllAsync(CancellationToken cancellationToken = default) =>
        (await db.Users.OrderBy(u => u.Id).ToListAsync(cancellationToken)).Select(ToResponse).ToList();

    public async Task<UserResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.FindAsync(new object?[] { id }, cancellationToken);
        return user is null ? null : ToResponse(user);
    }

    public async Task<UserResponse> CreateAsync(CreateUserRequest req, CancellationToken cancellationToken = default)
    {
        if (await db.Users.AnyAsync(u => u.Username == req.Username, cancellationToken))
            throw new DomainException($"Username '{req.Username}' is already taken.");

        var user = new User { Username = req.Username, Password = Hash(req.Password), Role = req.Role };
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(user);
    }

    public async Task<UserResponse?> UpdateAsync(int id, UpdateUserRequest req, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.FindAsync(new object?[] { id }, cancellationToken);
        if (user is null) return null;

        if (user.Username != req.Username &&
            await db.Users.AnyAsync(u => u.Username == req.Username, cancellationToken))
            throw new DomainException($"Username '{req.Username}' is already taken.");

        user.Username = req.Username;
        user.Role = req.Role;
        if (!string.IsNullOrEmpty(req.Password))
            user.Password = Hash(req.Password);

        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(user);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.FindAsync(new object?[] { id }, cancellationToken);
        if (user is null) return false;
        db.Users.Remove(user);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static UserResponse ToResponse(User u) => new(u.Id, u.Username, u.Role);
}
