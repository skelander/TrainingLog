using Microsoft.EntityFrameworkCore;
using TrainingLog.Data;
using TrainingLog.Models;

namespace TrainingLog.Services;

public class WorkoutTypesService(AppDbContext db) : IWorkoutTypesService
{
    public List<WorkoutType> GetAll() =>
        db.WorkoutTypes.Include(w => w.Fields).ToList();

    public WorkoutType? GetById(int id) =>
        db.WorkoutTypes.Include(w => w.Fields).FirstOrDefault(w => w.Id == id);

    public WorkoutType Create(string name, List<FieldDefinitionRequest> fields)
    {
        var type = new WorkoutType
        {
            Name = name,
            Fields = fields.Select(f => new FieldDefinition { Name = f.Name, Type = f.Type, Unit = f.Unit }).ToList()
        };
        db.WorkoutTypes.Add(type);
        db.SaveChanges();
        return type;
    }

    public WorkoutType? Update(int id, string name, List<FieldDefinitionRequest> fields)
    {
        var type = db.WorkoutTypes.Include(w => w.Fields).FirstOrDefault(w => w.Id == id);
        if (type is null) return null;

        type.Name = name;
        db.FieldDefinitions.RemoveRange(type.Fields);
        type.Fields = fields.Select(f => new FieldDefinition { Name = f.Name, Type = f.Type, Unit = f.Unit, WorkoutTypeId = id }).ToList();
        db.SaveChanges();
        return type;
    }

    public bool Delete(int id)
    {
        var type = db.WorkoutTypes.Find(id);
        if (type is null) return false;
        db.WorkoutTypes.Remove(type);
        db.SaveChanges();
        return true;
    }
}
