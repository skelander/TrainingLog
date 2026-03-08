# TrainingLog

A workout tracker built with ASP.NET Core. Admins define workout types and their tracked fields; users log sessions and view their history.

---

## API Endpoints

### `POST /auth/login`
Returns a signed JWT token (8-hour expiry). Rate-limited to 10 requests per minute per IP; returns 429 on excess.
```json
{ "username": "alice", "password": "alice" }
→ { "user": "alice", "role": "user", "token": "<jwt>" }
```

### `GET /workout-types`
Returns all workout types with their field definitions. Requires JWT.

### `GET /workout-types/{id}`
Returns a single workout type. Requires JWT.

### `POST /workout-types` *(admin)*
Creates a new workout type. Returns 409 if the name is already taken.
```json
{ "name": "Swimming", "fields": [{ "name": "Laps", "type": 0, "unit": null }] }
```
Field types: `0` = Number, `1` = Text, `2` = Duration

### `PUT /workout-types/{id}` *(admin)*
Replaces a workout type's name and fields. Returns 409 if the new name is already taken, or if existing sessions have logged values for this type.

### `DELETE /workout-types/{id}` *(admin)*
Deletes a workout type. Returns 409 if the type has existing workout sessions.

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
Constraints: `notes` ≤ 1000 characters; each `value` ≤ 500 characters.

### `GET /workouts/{id}`
Returns a single session. Owner or admin only.

### `PUT /workouts/{id}`
Updates a session's date, notes, and field values. Workout type cannot be changed. Owner or admin only.
```json
{
  "loggedAt": "2026-03-05T09:00:00Z",
  "notes": "Felt even better",
  "values": [
    { "fieldDefinitionId": 1, "value": "6.0" },
    { "fieldDefinitionId": 2, "value": "30:00" }
  ]
}
```

### `DELETE /workouts/{id}`
Deletes a session. Owner or admin only.

### `GET /users` *(admin)*
Returns all users (id, username, role — no password).

### `GET /users/{id}` *(admin)*
Returns a single user.

### `POST /users` *(admin)*
Creates a new user.
```json
{ "username": "carol", "password": "secret", "role": "user" }
```
Constraints: `username` 1–50 characters; `password` required, max 72 characters; `role` must be `"user"` or `"admin"`.

### `PUT /users/{id}` *(admin)*
Updates username, role, and optionally password. Omit or null `password` to keep it unchanged; max 72 characters if provided.

### `DELETE /users/{id}` *(admin)*
Deletes a user and all their workout sessions. Cannot delete your own account.

### `GET /health`
Public. Returns 200 OK if the service is up.

---

## Test accounts

Available in non-production environments only (not seeded in production).

| Username | Password | Role  |
|----------|----------|-------|
| `alice`  | `alice`  | user  |
| `bob`    | `bob`    | user  |
| `admin`  | `admin`  | admin |
| `1`      | `1`      | user  |

---

## Tech stack

- .NET 10 / ASP.NET Core MVC
- EF Core + SQLite
- xUnit + `WebApplicationFactory`
- Docker + Fly.io
- GitHub Actions (CI/CD)

---

## Running locally

```
dotnet run --project TrainingLog
dotnet test TrainingLog.Tests/TrainingLog.Tests.csproj
```
