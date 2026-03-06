using TrainingLog.Models;

namespace TrainingLog.Services;

public interface IWorkoutTypesService
{
    Task<List<WorkoutTypeResponse>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<WorkoutTypeResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<WorkoutTypeResponse> CreateAsync(string name, List<FieldDefinitionRequest> fields, CancellationToken cancellationToken = default);
    Task<WorkoutTypeResponse?> UpdateAsync(int id, string name, List<FieldDefinitionRequest> fields, CancellationToken cancellationToken = default);
    Task<bool?> DeleteAsync(int id, CancellationToken cancellationToken = default); // true=deleted, false=has dependent sessions, null=not found
}

public record FieldDefinitionRequest(string Name, FieldType Type, string? Unit);
