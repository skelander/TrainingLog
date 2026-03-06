using Microsoft.EntityFrameworkCore;
using TrainingLog.Models;

namespace TrainingLog.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<WorkoutType> WorkoutTypes => Set<WorkoutType>();
    public DbSet<FieldDefinition> FieldDefinitions => Set<FieldDefinition>();
    public DbSet<WorkoutSession> WorkoutSessions => Set<WorkoutSession>();
    public DbSet<FieldValue> FieldValues => Set<FieldValue>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();

        modelBuilder.Entity<WorkoutSession>()
            .HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<WorkoutSession>()
            .HasOne(s => s.WorkoutType)
            .WithMany()
            .HasForeignKey(s => s.WorkoutTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FieldValue>()
            .HasOne(v => v.FieldDefinition)
            .WithMany()
            .HasForeignKey(v => v.FieldDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FieldValue>()
            .Property(v => v.Value)
            .HasMaxLength(500);
    }
}
