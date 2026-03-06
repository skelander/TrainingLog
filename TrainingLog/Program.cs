using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using TrainingLog.Data;
using TrainingLog.Models;
using TrainingLog.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IWorkoutTypesService, WorkoutTypesService>();
builder.Services.AddScoped<IWorkoutsService, WorkoutsService>();
builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("GitHubPages", policy =>
        policy.WithOrigins("https://skelander.github.io")
              .AllowAnyMethod()
              .AllowAnyHeader());
});
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!);
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateLifetime = true,
        };
    });
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("login", httpContext =>
    {
        var config = httpContext.RequestServices.GetRequiredService<IConfiguration>();
        var permitLimit = config.GetValue<int>("RateLimit:LoginPermitLimit", 10);
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = permitLimit,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
            });
    });
    options.RejectionStatusCode = 429;
});
builder.Services.AddHealthChecks();
builder.Services.AddOpenApi();

var app = builder.Build();

// Apply migrations and seed initial data
using (var scope = app.Services.CreateScope())
{
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    if (string.IsNullOrEmpty(config["Jwt:Key"]))
        throw new InvalidOperationException("Jwt:Key configuration is required. Set the 'Jwt__Key' environment variable.");

    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    if (!db.Users.Any())
    {
        static string Hash(string pw) => BCrypt.Net.BCrypt.HashPassword(pw);
        db.Users.AddRange(
            new User { Username = "alice", Password = Hash("alice"), Role = "user" },
            new User { Username = "bob",   Password = Hash("bob"),   Role = "user" },
            new User { Username = "admin", Password = Hash("admin"), Role = "admin" },
            new User { Username = "1",     Password = Hash("1"),     Role = "user" }
        );
        db.WorkoutTypes.AddRange(
            new WorkoutType
            {
                Name = "Running",
                Fields =
                [
                    new FieldDefinition { Name = "Distance", Type = FieldType.Number, Unit = "km" },
                    new FieldDefinition { Name = "Duration", Type = FieldType.Duration },
                ]
            },
            new WorkoutType
            {
                Name = "BJJ",
                Fields =
                [
                    new FieldDefinition { Name = "Duration", Type = FieldType.Duration },
                    new FieldDefinition { Name = "Rounds",   Type = FieldType.Number },
                    new FieldDefinition { Name = "Notes",    Type = FieldType.Text },
                ]
            }
        );
        db.SaveChanges();
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseRateLimiter();
app.UseHttpsRedirection();
app.UseCors("GitHubPages");
app.UseAuthentication();
app.UseAuthorization();
app.MapHealthChecks("/health");
app.MapControllers();
app.Run();

public partial class Program { }
