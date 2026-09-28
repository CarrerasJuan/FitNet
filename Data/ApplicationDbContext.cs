using FitNet.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FitNet.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Membership> Memberships => Set<Membership>();
    public DbSet<MemberProfile> MemberProfiles => Set<MemberProfile>();
    public DbSet<TrainerProfile> TrainerProfiles => Set<TrainerProfile>();
    public DbSet<Exercise> Exercises => Set<Exercise>();
    public DbSet<WorkoutPlan> WorkoutPlans => Set<WorkoutPlan>();
    public DbSet<WorkoutExercise> WorkoutExercises => Set<WorkoutExercise>();
    public DbSet<WorkoutAssignment> WorkoutAssignments => Set<WorkoutAssignment>();
    public DbSet<ProgressRecord> ProgressRecords => Set<ProgressRecord>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ApplicationUser 1:1 MemberProfile
        builder.Entity<MemberProfile>(entity =>
        {
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasOne(e => e.User)
                  .WithOne(u => u.MemberProfile)
                  .HasForeignKey<MemberProfile>(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Membership)
                  .WithMany(m => m.Members)
                  .HasForeignKey(e => e.MembershipId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // ApplicationUser 1:1 TrainerProfile
        builder.Entity<TrainerProfile>(entity =>
        {
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.HasOne(e => e.User)
                  .WithOne(u => u.TrainerProfile)
                  .HasForeignKey<TrainerProfile>(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // WorkoutPlan 1:N WorkoutExercise N:1 Exercise (Relación N:M explícita)
        builder.Entity<WorkoutExercise>(entity =>
        {
            entity.HasOne(we => we.WorkoutPlan)
                  .WithMany(wp => wp.WorkoutExercises)
                  .HasForeignKey(we => we.WorkoutPlanId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(we => we.Exercise)
                  .WithMany(ex => ex.WorkoutExercises)
                  .HasForeignKey(we => we.ExerciseId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // WorkoutAssignment
        builder.Entity<WorkoutAssignment>(entity =>
        {
            entity.HasOne(wa => wa.Member)
                  .WithMany(m => m.Assignments)
                  .HasForeignKey(wa => wa.MemberId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(wa => wa.WorkoutPlan)
                  .WithMany(wp => wp.Assignments)
                  .HasForeignKey(wa => wa.WorkoutPlanId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(wa => wa.AssignedByTrainer)
                  .WithMany()
                  .HasForeignKey(wa => wa.AssignedByTrainerId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ProgressRecord (Archivos de progreso adicionales al avatar)
        builder.Entity<ProgressRecord>(entity =>
        {
            entity.Property(p => p.Weight).HasPrecision(5, 2);
            entity.Property(p => p.BodyFat).HasPrecision(4, 1);

            entity.HasOne(pr => pr.Member)
                  .WithMany(m => m.ProgressRecords)
                  .HasForeignKey(pr => pr.MemberId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Membership (Configuración de precios)
        builder.Entity<Membership>(entity =>
        {
            entity.Property(m => m.MonthlyPrice).HasPrecision(10, 2);
            entity.Property(m => m.SemiannualPrice).HasPrecision(10, 2);
        });

        // WorkoutPlan (Entrenador que creó la rutina)
        builder.Entity<WorkoutPlan>(entity =>
        {
            entity.HasOne(wp => wp.Trainer)
                  .WithMany()
                  .HasForeignKey(wp => wp.TrainerId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
