using System.Globalization;
using Microsoft.EntityFrameworkCore;
using TrainingLog.Data;
using TrainingLog.Models;

namespace TrainingLog.Services;

public class WorkoutsService(AppDbContext db) : IWorkoutsService
{
    public async Task<List<WorkoutSessionResponse>> GetForUserAsync(int userId, CancellationToken cancellationToken = default) =>
        (await db.WorkoutSessions
            .Include(s => s.User)
            .Include(s => s.WorkoutType)
            .Include(s => s.Values).ThenInclude(v => v.FieldDefinition)
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.LoggedAt)
            .ToListAsync(cancellationToken))
            .Select(ToResponse)
            .ToList();

    public async Task<WorkoutSessionResponse?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var session = await db.WorkoutSessions
            .Include(s => s.User)
            .Include(s => s.WorkoutType)
            .Include(s => s.Values).ThenInclude(v => v.FieldDefinition)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        return session is null ? null : ToResponse(session);
    }

    public async Task<WorkoutSessionResponse?> CreateAsync(CreateSessionRequest request, CancellationToken cancellationToken = default)
    {
        if (!await db.WorkoutTypes.AnyAsync(t => t.Id == request.WorkoutTypeId, cancellationToken)) return null;

        if (request.Values.Count > 0)
        {
            var fieldDefs = (await db.FieldDefinitions
                .Where(f => f.WorkoutTypeId == request.WorkoutTypeId)
                .ToListAsync(cancellationToken))
                .ToDictionary(f => f.Id);
            if (request.Values.Any(v => !fieldDefs.ContainsKey(v.FieldDefinitionId)))
                throw new DomainException("One or more field definition IDs do not belong to the specified workout type.");
            foreach (var v in request.Values)
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
        await db.Entry(session).Reference(s => s.User).LoadAsync(cancellationToken);
        await db.Entry(session).Reference(s => s.WorkoutType).LoadAsync(cancellationToken);
        await db.Entry(session).Collection(s => s.Values).Query().Include(v => v.FieldDefinition).LoadAsync(cancellationToken);
        return ToResponse(session);
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
