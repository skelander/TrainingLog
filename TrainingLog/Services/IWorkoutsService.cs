using TrainingLog.Models;

namespace TrainingLog.Services;

public interface IWorkoutsService
{
    List<WorkoutSession> GetForUser(string username);
    WorkoutSession? GetById(int id);
    WorkoutSession? Create(string username, int workoutTypeId, DateTime loggedAt, string? notes, List<FieldValueRequest> values);
    bool Delete(int id, string username, bool isAdmin);
}

public record FieldValueRequest(int FieldDefinitionId, string Value);
