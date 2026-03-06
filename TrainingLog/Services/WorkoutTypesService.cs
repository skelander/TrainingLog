using Microsoft.EntityFrameworkCore;
using TrainingLog.Data;
using TrainingLog.Models;

namespace TrainingLog.Services;

public class WorkoutTypesService(AppDbContext db) : IWorkoutTypesService
{
    public async Task<List<WorkoutTypeResponse>> GetAllAsync() =>
        (await db.WorkoutTypes.Include(w => w.Fields).ToListAsync()).Select(ToResponse).ToList();

    public async Task<WorkoutTypeResponse?> GetByIdAsync(int id)
    {
        var type = await db.WorkoutTypes.Include(w => w.Fields).FirstOrDefaultAsync(w => w.Id == id);
        return type is null ? null : ToResponse(type);
    }

    public async Task<WorkoutTypeResponse> CreateAsync(string name, List<FieldDefinitionRequest> fields)
    {
        var type = new WorkoutType
        {
            Name = name,
            Fields = fields.Select(f => new FieldDefinition { Name = f.Name, Type = f.Type, Unit = f.Unit }).ToList()
        };
        db.WorkoutTypes.Add(type);
        await db.SaveChangesAsync();
        return ToResponse(type);
    }

    public async Task<WorkoutTypeResponse?> UpdateAsync(int id, string name, List<FieldDefinitionRequest> fields)
    {
        var type = await db.WorkoutTypes.Include(w => w.Fields).FirstOrDefaultAsync(w => w.Id == id);
        if (type is null) return null;

        type.Name = name;

        var fieldIds = type.Fields.Select(f => f.Id).ToHashSet();
        if (await db.FieldValues.AnyAsync(v => fieldIds.Contains(v.FieldDefinitionId)))
            throw new InvalidOperationException(
                "Cannot update fields: existing workout sessions have logged values for this type. Delete those sessions first.");

        db.FieldDefinitions.RemoveRange(type.Fields);
        type.Fields = fields.Select(f => new FieldDefinition { Name = f.Name, Type = f.Type, Unit = f.Unit, WorkoutTypeId = id }).ToList();
        await db.SaveChangesAsync();
        return ToResponse(type);
    }

    public async Task<bool?> DeleteAsync(int id)
    {
        var type = await db.WorkoutTypes.FindAsync(id);
        if (type is null) return null;
        if (await db.WorkoutSessions.AnyAsync(s => s.WorkoutTypeId == id)) return false;
        db.WorkoutTypes.Remove(type);
        await db.SaveChangesAsync();
        return true;
    }

    private static WorkoutTypeResponse ToResponse(WorkoutType t) =>
        new(t.Id, t.Name, t.Fields.Select(f => new FieldDefResponse(f.Id, f.Name, f.Type, f.Unit)).ToList());
}
