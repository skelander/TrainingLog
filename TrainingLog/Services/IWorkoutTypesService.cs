using TrainingLog.Models;

namespace TrainingLog.Services;

public interface IWorkoutTypesService
{
    List<WorkoutTypeResponse> GetAll();
    WorkoutTypeResponse? GetById(int id);
    WorkoutTypeResponse Create(string name, List<FieldDefinitionRequest> fields);
    WorkoutTypeResponse? Update(int id, string name, List<FieldDefinitionRequest> fields);
    bool Delete(int id);
}

public record FieldDefinitionRequest(string Name, FieldType Type, string? Unit);
