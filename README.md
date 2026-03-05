# TrainingLog

A workout tracker built with ASP.NET Core. Admins define workout types and their tracked fields; users log sessions and view their history.

---

## API Endpoints

### `POST /auth/login`
Returns a signed JWT token (8-hour expiry).
```json
{ "username": "alice", "password": "alice" }
→ { "user": "alice", "role": "user", "token": "<jwt>" }
```

### `GET /workout-types`
Returns all workout types with their field definitions. Requires JWT.

### `GET /workout-types/{id}`
Returns a single workout type. Requires JWT.

### `POST /workout-types` *(admin)*
Creates a new workout type.
```json
{ "name": "Swimming", "fields": [{ "name": "Laps", "type": 0, "unit": null }] }
```
Field types: `0` = Number, `1` = Text, `2` = Duration

### `PUT /workout-types/{id}` *(admin)*
Replaces a workout type's name and fields.

### `DELETE /workout-types/{id}` *(admin)*
Deletes a workout type.

### `GET /workouts`
Returns the calling user's workout sessions, newest first. Requires JWT.

### `POST /workouts`
Logs a new workout session. Requires JWT.
```json
{
  "workoutTypeId": 1,
  "loggedAt": "2026-03-05T08:00:00Z",
  "notes": "Felt great",
  "values": [
    { "fieldDefinitionId": 1, "value": "5.2" },
    { "fieldDefinitionId": 2, "value": "28:00" }
  ]
}
```

### `GET /workouts/{id}`
Returns a single session. Owner or admin only.

### `DELETE /workouts/{id}`
Deletes a session. Owner or admin only.

---

## Test accounts

| Username | Password | Role  |
|----------|----------|-------|
| `alice`  | `alice`  | user  |
| `bob`    | `bob`    | user  |
| `admin`  | `admin`  | admin |

---

## Tech stack

- .NET 10 / ASP.NET Core MVC
- EF Core (in-memory database)
- xUnit + `WebApplicationFactory`
- Docker + Fly.io
- GitHub Actions (CI/CD)

---

## Running locally

```
dotnet run --project TrainingLog
dotnet test TrainingLog.Tests/TrainingLog.Tests.csproj
```
