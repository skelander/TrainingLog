using Microsoft.EntityFrameworkCore;
using TrainingLog.Data;
using TrainingLog.Models;

namespace TrainingLog.Services;

public class WorkoutTypesService(AppDbContext db) : IWorkoutTypesService
{
    public async Task<List<WorkoutTypeResponse>> GetAllAsync(CancellationToken cancellationToken = default) =>
        (await db.WorkoutTypes.Include(w => w.Fields).ToListAsync(cancellationToken)).Select(ToResponse).ToList();

    public async Task<WorkoutTypeResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var type = await db.WorkoutTypes.Include(w => w.Fields).FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
        return type is null ? null : ToResponse(type);
    }

    public async Task<WorkoutTypeResponse> CreateAsync(string name, List<FieldDefinitionRequest> fields, CancellationToken cancellationToken = default)
    {
        var type = new WorkoutType
        {
            Name = name,
            Fields = fields.Select(f => new FieldDefinition { Name = f.Name, Type = f.Type, Unit = f.Unit }).ToList()
        };
        db.WorkoutTypes.Add(type);
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(type);
    }

    public async Task<WorkoutTypeResponse?> UpdateAsync(int id, string name, List<FieldDefinitionRequest> fields, CancellationToken cancellationToken = default)
    {
        var type = await db.WorkoutTypes.Include(w => w.Fields).FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
        if (type is null) return null;

        type.Name = name;
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException) { throw new DomainException("The resource was modified concurrently. Please retry."); }

        var fieldIds = type.Fields.Select(f => f.Id).ToHashSet();
        if (await db.FieldValues.AnyAsync(v => fieldIds.Contains(v.FieldDefinitionId), cancellationToken))
            throw new DomainException(
                "Cannot update fields: existing workout sessions have logged values for this type. Delete those sessions first.");

        db.FieldDefinitions.RemoveRange(type.Fields);
        type.Fields = fields.Select(f => new FieldDefinition { Name = f.Name, Type = f.Type, Unit = f.Unit, WorkoutTypeId = id }).ToList();
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException) { throw new DomainException("The resource was modified concurrently. Please retry."); }
        return ToResponse(type);
    }

    public async Task<bool?> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var type = await db.WorkoutTypes.FindAsync(new object?[] { id }, cancellationToken);
        if (type is null) return null;
        if (await db.WorkoutSessions.AnyAsync(s => s.WorkoutTypeId == id, cancellationToken)) return false;
        db.WorkoutTypes.Remove(type);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static WorkoutTypeResponse ToResponse(WorkoutType t) =>
        new(t.Id, t.Name, t.Fields.Select(f => new FieldDefResponse(f.Id, f.Name, f.Type, f.Unit)).ToList());
}
