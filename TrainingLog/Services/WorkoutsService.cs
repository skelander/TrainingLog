using Microsoft.EntityFrameworkCore;
using TrainingLog.Data;
using TrainingLog.Models;

namespace TrainingLog.Services;

public class WorkoutsService(AppDbContext db) : IWorkoutsService
{
    public List<WorkoutSessionResponse> GetForUser(int userId) =>
        db.WorkoutSessions
            .Include(s => s.User)
            .Include(s => s.WorkoutType)
            .Include(s => s.Values).ThenInclude(v => v.FieldDefinition)
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.LoggedAt)
            .ToList()
            .Select(ToResponse)
            .ToList();

    public WorkoutSessionResponse? GetById(int id)
    {
        var session = db.WorkoutSessions
            .Include(s => s.User)
            .Include(s => s.WorkoutType)
            .Include(s => s.Values).ThenInclude(v => v.FieldDefinition)
            .FirstOrDefault(s => s.Id == id);
        return session is null ? null : ToResponse(session);
    }

    public WorkoutSessionResponse? Create(int userId, int workoutTypeId, DateTime loggedAt, string? notes, List<FieldValueRequest> values)
    {
        if (!db.WorkoutTypes.Any(t => t.Id == workoutTypeId)) return null;

        var session = new WorkoutSession
        {
            UserId = userId,
            WorkoutTypeId = workoutTypeId,
            LoggedAt = loggedAt,
            Notes = notes,
            Values = values.Select(v => new FieldValue { FieldDefinitionId = v.FieldDefinitionId, Value = v.Value }).ToList()
        };
        db.WorkoutSessions.Add(session);
        db.SaveChanges();
        return GetById(session.Id);
    }

    public bool Delete(int id, int userId, bool isAdmin)
    {
        var session = db.WorkoutSessions.Find(id);
        if (session is null) return false;
        if (!isAdmin && session.UserId != userId) return false;
        db.WorkoutSessions.Remove(session);
        db.SaveChanges();
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
