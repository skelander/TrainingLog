using Microsoft.EntityFrameworkCore;
using TrainingLog.Data;
using TrainingLog.Models;

namespace TrainingLog.Services;

public class WorkoutsService(AppDbContext db) : IWorkoutsService
{
    public async Task<List<WorkoutSessionResponse>> GetForUserAsync(int userId) =>
        (await db.WorkoutSessions
            .Include(s => s.User)
            .Include(s => s.WorkoutType)
            .Include(s => s.Values).ThenInclude(v => v.FieldDefinition)
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.LoggedAt)
            .ToListAsync())
            .Select(ToResponse)
            .ToList();

    public async Task<WorkoutSessionResponse?> GetByIdAsync(int id)
    {
        var session = await db.WorkoutSessions
            .Include(s => s.User)
            .Include(s => s.WorkoutType)
            .Include(s => s.Values).ThenInclude(v => v.FieldDefinition)
            .FirstOrDefaultAsync(s => s.Id == id);
        return session is null ? null : ToResponse(session);
    }

    public async Task<WorkoutSessionResponse?> CreateAsync(int userId, int workoutTypeId, DateTime loggedAt, string? notes, List<FieldValueRequest> values)
    {
        if (!await db.WorkoutTypes.AnyAsync(t => t.Id == workoutTypeId)) return null;

        if (values.Count > 0)
        {
            var validFieldIds = (await db.FieldDefinitions
                .Where(f => f.WorkoutTypeId == workoutTypeId)
                .Select(f => f.Id)
                .ToListAsync())
                .ToHashSet();
            if (values.Any(v => !validFieldIds.Contains(v.FieldDefinitionId)))
                throw new InvalidOperationException("One or more field definition IDs do not belong to the specified workout type.");
        }

        var session = new WorkoutSession
        {
            UserId = userId,
            WorkoutTypeId = workoutTypeId,
            LoggedAt = loggedAt,
            Notes = notes,
            Values = values.Select(v => new FieldValue { FieldDefinitionId = v.FieldDefinitionId, Value = v.Value }).ToList()
        };
        db.WorkoutSessions.Add(session);
        await db.SaveChangesAsync();
        return await GetByIdAsync(session.Id);
    }

    public async Task<bool?> DeleteAsync(int id, int userId, bool isAdmin)
    {
        var session = await db.WorkoutSessions.FindAsync(id);
        if (session is null) return null;
        if (!isAdmin && session.UserId != userId) return false;
        db.WorkoutSessions.Remove(session);
        await db.SaveChangesAsync();
        return true;
    }

    private static WorkoutSessionResponse ToResponse(WorkoutSession s) =>
        new(s.Id,
            s.UserId,
            s.User!.Username,
            s.WorkoutTypeId,
            s.WorkoutType?.Name ?? string.Empty,
            s.LoggedAt,
            s.Notes,
            s.Values.Select(v => new FieldValueResponse(
                v.Id,
                v.FieldDefinitionId,
                v.FieldDefinition?.Name ?? string.Empty,
                v.Value)).ToList());
}
