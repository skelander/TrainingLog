using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TrainingLog.Data;
using TrainingLog.Models;

namespace TrainingLog.Services;

public class WorkoutsService(AppDbContext db) : IWorkoutsService
{
    private IQueryable<WorkoutSession> SessionQuery() =>
        db.WorkoutSessions
            .Include(s => s.User)
            .Include(s => s.WorkoutType)
            .Include(s => s.Values).ThenInclude(v => v.FieldDefinition);

    public async Task<List<WorkoutSessionResponse>> GetForUserAsync(int userId, CancellationToken cancellationToken = default) =>
        (await SessionQuery()
            .Where(s => s.UserId == userId)
            .ToListAsync(cancellationToken))
            .OrderByDescending(s => s.LoggedAt)
            .Select(ToResponse)
            .ToList();

    public async Task<WorkoutSessionResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var session = await SessionQuery().FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        return session is null ? null : ToResponse(session);
    }

    public async Task<WorkoutSessionResponse?> CreateAsync(CreateSessionRequest request, CancellationToken cancellationToken = default)
    {
        if (!await db.WorkoutTypes.AnyAsync(t => t.Id == request.WorkoutTypeId, cancellationToken)) return null;

        await ValidateFieldValuesAsync(request.WorkoutTypeId, request.Values, cancellationToken);

        var session = new WorkoutSession
        {
            UserId = request.UserId,
            WorkoutTypeId = request.WorkoutTypeId,
            LoggedAt = request.LoggedAt,
            Notes = request.Notes,
            Values = request.Values.Select(v => new FieldValue { FieldDefinitionId = v.FieldDefinitionId, Value = v.Value }).ToList()
        };
        db.WorkoutSessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(await SessionQuery().FirstAsync(s => s.Id == session.Id, cancellationToken));
    }

    public async Task<WorkoutSessionResponse?> UpdateAsync(int id, int userId, bool isAdmin, UpdateSessionRequest request, CancellationToken cancellationToken = default)
    {
        var session = await db.WorkoutSessions
            .Include(s => s.Values)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (session is null) return null;
        if (!isAdmin && session.UserId != userId) return null;

        await ValidateFieldValuesAsync(session.WorkoutTypeId, request.Values, cancellationToken);

        db.FieldValues.RemoveRange(session.Values);
        session.LoggedAt = request.LoggedAt;
        session.Notes = request.Notes;
        session.Values = request.Values.Select(v => new FieldValue { FieldDefinitionId = v.FieldDefinitionId, Value = v.Value }).ToList();
        await db.SaveChangesAsync(cancellationToken);
        return ToResponse(await SessionQuery().FirstAsync(s => s.Id == id, cancellationToken));
    }

    public async Task<bool?> DeleteAsync(int id, int userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var session = await db.WorkoutSessions.FindAsync(new object?[] { id }, cancellationToken);
        if (session is null) return null;
        if (!isAdmin && session.UserId != userId) return false;
        db.WorkoutSessions.Remove(session);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task ValidateFieldValuesAsync(int workoutTypeId, List<FieldValueRequest> values, CancellationToken cancellationToken)
    {
        if (values.Count == 0) return;
        var fieldDefs = (await db.FieldDefinitions
            .Where(f => f.WorkoutTypeId == workoutTypeId)
            .ToListAsync(cancellationToken))
            .ToDictionary(f => f.Id);
        if (values.Any(v => !fieldDefs.ContainsKey(v.FieldDefinitionId)))
            throw new DomainException("One or more field definition IDs do not belong to the specified workout type.");
        foreach (var v in values)
        {
            var def = fieldDefs[v.FieldDefinitionId];
            var valid = def.Type switch
            {
                FieldType.Number   => decimal.TryParse(v.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out _),
                FieldType.Duration => TimeSpan.TryParse(v.Value, out _),
                _                  => true,
            };
            if (!valid)
                throw new DomainException($"Value '{v.Value}' is not valid for field '{def.Name}' (expected {def.Type}).");
        }
    }

    private static WorkoutSessionResponse ToResponse(WorkoutSession s) =>
        new(s.Id,
            s.UserId,
            s.User!.Username,
            s.WorkoutTypeId,
            s.WorkoutType!.Name,
            s.LoggedAt,
            s.Notes,
            s.Values.Select(v => new FieldValueResponse(
                v.Id,
                v.FieldDefinitionId,
                v.FieldDefinition!.Name,
                v.Value)).ToList());
}
