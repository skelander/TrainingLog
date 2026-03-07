namespace TrainingLog.Models;

public record FieldDefResponse(int Id, string Name, FieldType Type, string? Unit);
public record WorkoutTypeResponse(int Id, string Name, List<FieldDefResponse> Fields);

public record FieldValueResponse(int Id, int FieldDefinitionId, string FieldDefinitionName, string Value);
public record WorkoutSessionResponse(
    int Id,
    int UserId,
    string Username,
    int WorkoutTypeId,
    string WorkoutTypeName,
    DateTimeOffset LoggedAt,
    string? Notes,
    List<FieldValueResponse> Values);
