namespace TrainingLog.Models;

public class WorkoutType
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public List<FieldDefinition> Fields { get; set; } = [];
}

public class FieldDefinition
{
    public int Id { get; set; }
    public int WorkoutTypeId { get; set; }
    public required string Name { get; set; }
    public FieldType Type { get; set; }
    public string? Unit { get; set; }
}

public enum FieldType
{
    Number,
    Text,
    Duration
}
