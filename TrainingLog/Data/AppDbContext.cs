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
        modelBuilder.Entity<User>().Property(u => u.Username).HasMaxLength(50);
        modelBuilder.Entity<User>().Property(u => u.Password).HasMaxLength(60);

        modelBuilder.Entity<WorkoutType>().HasIndex(t => t.Name).IsUnique();
        modelBuilder.Entity<WorkoutType>().Property(t => t.Name).HasMaxLength(100);

        modelBuilder.Entity<WorkoutSession>()
            .HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<WorkoutSession>()
            .HasOne(s => s.WorkoutType)
            .WithMany()
            .HasForeignKey(s => s.WorkoutTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<WorkoutSession>()
            .Property(s => s.Notes).HasMaxLength(1000);

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
