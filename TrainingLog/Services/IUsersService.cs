namespace TrainingLog.Services;

public record UserResponse(int Id, string Username, string Role);
public record CreateUserRequest(string Username, string Password, string Role);
public record UpdateUserRequest(string Username, string? Password, string Role);

public interface IUsersService
{
    Task<List<UserResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<UserResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<UserResponse> CreateAsync(CreateUserRequest req, CancellationToken cancellationToken = default);
    Task<UserResponse?> UpdateAsync(int id, UpdateUserRequest req, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
