using TrainingLog.Models;

namespace TrainingLog.Services;

public interface IWorkoutsService
{
    List<WorkoutSessionResponse> GetForUser(int userId);
    WorkoutSessionResponse? GetById(int id);
    WorkoutSessionResponse? Create(int userId, int workoutTypeId, DateTime loggedAt, string? notes, List<FieldValueRequest> values);
    bool Delete(int id, int userId, bool isAdmin);
}

public record FieldValueRequest(int FieldDefinitionId, string Value);
