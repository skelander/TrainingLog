using TrainingLog.Models;

namespace TrainingLog.Services;

public interface IWorkoutsService
{
    Task<List<WorkoutSessionResponse>> GetForUserAsync(int userId);
    Task<WorkoutSessionResponse?> GetByIdAsync(int id);
    Task<WorkoutSessionResponse?> CreateAsync(int userId, int workoutTypeId, DateTime loggedAt, string? notes, List<FieldValueRequest> values);
    Task<bool?> DeleteAsync(int id, int userId, bool isAdmin); // true=deleted, false=forbidden, null=not found
}

public record FieldValueRequest(int FieldDefinitionId, string Value);
