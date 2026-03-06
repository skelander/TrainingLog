using TrainingLog.Models;

namespace TrainingLog.Services;

public interface IWorkoutsService
{
    Task<List<WorkoutSessionResponse>> GetForUserAsync(int userId, CancellationToken cancellationToken = default);
    Task<WorkoutSessionResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<WorkoutSessionResponse?> CreateAsync(int userId, int workoutTypeId, DateTime loggedAt, string? notes, List<FieldValueRequest> values, CancellationToken cancellationToken = default);
    Task<bool?> DeleteAsync(int id, int userId, bool isAdmin, CancellationToken cancellationToken = default); // true=deleted, false=forbidden, null=not found
}

public record FieldValueRequest(int FieldDefinitionId, string Value);
