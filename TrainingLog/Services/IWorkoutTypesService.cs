using TrainingLog.Models;

namespace TrainingLog.Services;

public interface IWorkoutTypesService
{
    Task<List<WorkoutTypeResponse>> GetAllAsync();
    Task<WorkoutTypeResponse?> GetByIdAsync(int id);
    Task<WorkoutTypeResponse> CreateAsync(string name, List<FieldDefinitionRequest> fields);
    Task<WorkoutTypeResponse?> UpdateAsync(int id, string name, List<FieldDefinitionRequest> fields);
    Task<bool?> DeleteAsync(int id); // true=deleted, false=has dependent sessions, null=not found
}

public record FieldDefinitionRequest(string Name, FieldType Type, string? Unit);
