using Microsoft.EntityFrameworkCore;
using TrainingLog.Data;
using TrainingLog.Models;

namespace TrainingLog.Services;

public class WorkoutsService(AppDbContext db) : IWorkoutsService
{
    public List<WorkoutSession> GetForUser(string username) =>
        db.WorkoutSessions
            .Include(s => s.WorkoutType).ThenInclude(t => t!.Fields)
            .Include(s => s.Values).ThenInclude(v => v.FieldDefinition)
            .Where(s => s.Username == username)
            .OrderByDescending(s => s.LoggedAt)
            .ToList();

    public WorkoutSession? GetById(int id) =>
        db.WorkoutSessions
            .Include(s => s.WorkoutType).ThenInclude(t => t!.Fields)
            .Include(s => s.Values).ThenInclude(v => v.FieldDefinition)
            .FirstOrDefault(s => s.Id == id);

    public WorkoutSession? Create(string username, int workoutTypeId, DateTime loggedAt, string? notes, List<FieldValueRequest> values)
    {
        if (!db.WorkoutTypes.Any(t => t.Id == workoutTypeId)) return null;

        var session = new WorkoutSession
        {
            Username = username,
            WorkoutTypeId = workoutTypeId,
            LoggedAt = loggedAt,
            Notes = notes,
            Values = values.Select(v => new FieldValue { FieldDefinitionId = v.FieldDefinitionId, Value = v.Value }).ToList()
        };
        db.WorkoutSessions.Add(session);
        db.SaveChanges();
        return GetById(session.Id);
    }

    public bool Delete(int id, string username, bool isAdmin)
    {
        var session = db.WorkoutSessions.Find(id);
        if (session is null) return false;
        if (!isAdmin && session.Username != username) return false;
        db.WorkoutSessions.Remove(session);
        db.SaveChanges();
        return true;
    }
}
