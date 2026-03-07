namespace TrainingLog.Models;

public class WorkoutSession
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
    public int WorkoutTypeId { get; set; }
    public WorkoutType? WorkoutType { get; set; }
    public DateTimeOffset LoggedAt { get; set; }
    public string? Notes { get; set; }
    public List<FieldValue> Values { get; set; } = [];
}

public class FieldValue
{
    public int Id { get; set; }
    public int WorkoutSessionId { get; set; }
    public int FieldDefinitionId { get; set; }
    public FieldDefinition? FieldDefinition { get; set; }
    public required string Value { get; set; }
}
