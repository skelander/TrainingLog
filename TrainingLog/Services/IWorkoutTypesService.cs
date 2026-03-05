using TrainingLog.Models;

namespace TrainingLog.Services;

public interface IWorkoutTypesService
{
    List<WorkoutType> GetAll();
    WorkoutType? GetById(int id);
    WorkoutType Create(string name, List<FieldDefinitionRequest> fields);
    WorkoutType? Update(int id, string name, List<FieldDefinitionRequest> fields);
    bool Delete(int id);
}

public record FieldDefinitionRequest(string Name, FieldType Type, string? Unit);
