# TrainingLog

ASP.NET Core REST API for tracking workouts. Admins define workout types and their fields; users log sessions.

## Tech Stack
- .NET 10 / ASP.NET Core MVC
- EF Core + SQLite
- JWT Bearer authentication (HMAC-SHA256, 8-hour expiry)
- xUnit + `WebApplicationFactory` for integration tests
- Docker + Fly.io (API)
- GitHub Pages (frontend)
- GitHub Actions (CI/CD)

## Project Structure
```
TrainingLog/
  Controllers/   AuthController, WorkoutTypesController, WorkoutsController, UsersController, ValidateUserIdAttribute
  Data/          AppDbContext
  Models/        User, WorkoutType, FieldDefinition, FieldType, WorkoutSession, FieldValue,
                 WorkoutTypeResponse, WorkoutSessionResponse, FieldDefResponse, FieldValueResponse
  Services/      IAuthService, AuthService, IWorkoutTypesService, WorkoutTypesService,
                 IWorkoutsService, WorkoutsService, IUsersService, UsersService, DomainException
  Limits.cs      Validation constants (PasswordMaxLength, UsernameMaxLength, WorkoutTypeNameMaxLength,
                 NotesMaxLength, FieldValueMaxLength)

TrainingLog.Tests/
  AuthControllerTests.cs
  WorkoutTypesControllerTests.cs
  WorkoutsControllerTests.cs
  UsersControllerTests.cs
  Helpers.cs              GetTokenAsync, WithToken extension
  TrainingLogFactory.cs   WebApplicationFactory subclass (SQLite in-memory, JWT test key, rate limit override, BCrypt work factor = 4)
```

## Architecture
- Controllers depend on interfaces only
- Services are Scoped (use AppDbContext)
- JWT key injected at runtime from env var (Fly.io secret in production; appsettings.json fallback for dev)

## Seeded Data
Users are only seeded in non-production environments (Development/test).
Workout types are always seeded if none exist.

| Username | Password | Role  | Env          |
|----------|----------|-------|--------------|
| alice    | alice    | user  | non-prod only |
| bob      | bob      | user  | non-prod only |
| admin    | admin    | admin | non-prod only |
| 1        | 1        | user  | non-prod only |

Workout types (all envs):
- Running: Distance (Number, km), Duration (Duration)
- BJJ: Duration (Duration), Rounds (Number), Notes (Text)

**Production first deploy**: no users are seeded — create the first admin via a migration or direct DB access.

## Auth
- `POST /auth/login` → JWT token
- `GET /health` → public
- All other endpoints require auth
- Admin-only: POST/PUT/DELETE /workout-types; all of GET/POST/PUT/DELETE /users
- Users see only their own workouts (GET /workouts); admin can view any session via GET /workouts/{id}
- Deleting a user cascade-deletes their workout sessions

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

## Frontend (GitHub Pages)
- URL: https://skelander.github.io/TrainingLog/
- Source: `docs/` folder on main branch
- Entry: `docs/index.html` — pure HTML/CSS/JS, no build step
- Deployed via `.github/workflows/pages.yml`
- **Setup**: repo Settings → Pages → Source → GitHub Actions (one-time manual step)

## Workflow
- Deploy directly to main; hide in-progress features behind toggles if needed
