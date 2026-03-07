using TrainingLog.Models;

namespace TrainingLog.Services;

public interface IWorkoutsService
{
    Task<List<WorkoutSessionResponse>> GetForUserAsync(int userId, CancellationToken cancellationToken = default);
    Task<WorkoutSessionResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<WorkoutSessionResponse?> CreateAsync(CreateSessionRequest request, CancellationToken cancellationToken = default);
    Task<bool?> DeleteAsync(int id, int userId, bool isAdmin, CancellationToken cancellationToken = default); // true=deleted, false=forbidden, null=not found
}

public record FieldValueRequest(int FieldDefinitionId, string Value);
public record CreateSessionRequest(int UserId, int WorkoutTypeId, DateTimeOffset LoggedAt, string? Notes, List<FieldValueRequest> Values);
