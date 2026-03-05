# TrainingLog

ASP.NET Core REST API for tracking workouts. Admins define workout types and their fields; users log sessions.

## Tech Stack
- .NET 10 / ASP.NET Core MVC
- EF Core (in-memory database)
- JWT Bearer authentication (HMAC-SHA256, 8-hour expiry)
- xUnit + `WebApplicationFactory` for integration tests
- Docker + Fly.io (API)
- GitHub Pages (frontend, coming later)
- GitHub Actions (CI/CD)

## Project Structure
```
TrainingLog/
  Controllers/   AuthController, WorkoutTypesController, WorkoutsController
  Data/          AppDbContext
  Models/        User, WorkoutType, FieldDefinition, FieldType, WorkoutSession, FieldValue
  Services/      IAuthService, AuthService, IWorkoutTypesService, WorkoutTypesService,
                 IWorkoutsService, WorkoutsService

TrainingLog.Tests/
  AuthControllerTests.cs
  WorkoutTypesControllerTests.cs
  WorkoutsControllerTests.cs
  Helpers.cs     GetTokenAsync, WithToken extension
```

## Architecture
- Controllers depend on interfaces only
- Services are Scoped (use AppDbContext)
- JWT key injected at runtime from env var (Fly.io secret in production; appsettings.json fallback for dev)

## Seeded Users
| Username | Password | Role  |
|----------|----------|-------|
| alice    | alice    | user  |
| bob      | bob      | user  |
| admin    | admin    | admin |

## Seeded Workout Types
- Running: Distance (Number, km), Duration (Duration)
- BJJ: Duration (Duration), Rounds (Number), Notes (Text)

## Auth
- `POST /auth/login` → JWT token
- All endpoints require auth
- Admin-only: POST/PUT/DELETE /workout-types
- Users see only their own workouts; admin can see all

## Code Style
- C# primary constructors
- Nullable enabled

## Running Locally
```
dotnet run --project TrainingLog
dotnet test TrainingLog.Tests/TrainingLog.Tests.csproj
```

## dotnet path
`C:\Program Files\dotnet\dotnet.exe` (not on PATH in bash — use PowerShell)

## Workflow
- Deploy directly to main; hide in-progress features behind toggles if needed
