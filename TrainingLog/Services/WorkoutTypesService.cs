using Microsoft.EntityFrameworkCore;
using TrainingLog.Data;
using TrainingLog.Models;

namespace TrainingLog.Services;

public class WorkoutTypesService(AppDbContext db) : IWorkoutTypesService
{
    public List<WorkoutTypeResponse> GetAll() =>
        db.WorkoutTypes.Include(w => w.Fields).ToList().Select(ToResponse).ToList();

    public WorkoutTypeResponse? GetById(int id)
    {
        var type = db.WorkoutTypes.Include(w => w.Fields).FirstOrDefault(w => w.Id == id);
        return type is null ? null : ToResponse(type);
    }

    public WorkoutTypeResponse Create(string name, List<FieldDefinitionRequest> fields)
    {
        var type = new WorkoutType
        {
            Name = name,
            Fields = fields.Select(f => new FieldDefinition { Name = f.Name, Type = f.Type, Unit = f.Unit }).ToList()
        };
        db.WorkoutTypes.Add(type);
        db.SaveChanges();
        return ToResponse(type);
    }

    public WorkoutTypeResponse? Update(int id, string name, List<FieldDefinitionRequest> fields)
    {
        var type = db.WorkoutTypes.Include(w => w.Fields).FirstOrDefault(w => w.Id == id);
        if (type is null) return null;

        type.Name = name;
        db.FieldDefinitions.RemoveRange(type.Fields);
        type.Fields = fields.Select(f => new FieldDefinition { Name = f.Name, Type = f.Type, Unit = f.Unit, WorkoutTypeId = id }).ToList();
        db.SaveChanges();
        return ToResponse(type);
    }

    public bool Delete(int id)
    {
        var type = db.WorkoutTypes.Find(id);
        if (type is null) return false;
        db.WorkoutTypes.Remove(type);
        db.SaveChanges();
        return true;
    }

    private static WorkoutTypeResponse ToResponse(WorkoutType t) =>
        new(t.Id, t.Name, t.Fields.Select(f => new FieldDefResponse(f.Id, f.Name, f.Type, f.Unit)).ToList());
}
