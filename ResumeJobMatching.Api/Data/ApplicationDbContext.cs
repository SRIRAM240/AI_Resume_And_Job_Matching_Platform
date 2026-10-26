using Microsoft.EntityFrameworkCore;
using ResumeJobMatching.Api.Models;
using ResumeJobMatching.Api.Models.Jobs;

namespace ResumeJobMatching.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Resume> Resumes => Set<Resume>();
    public DbSet<Job> Jobs => Set<Job>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);

            entity.HasIndex(u => u.Email)
                  .IsUnique();

            entity.Property(u => u.FullName)
                  .HasMaxLength(100)
                  .IsRequired();

            entity.Property(u => u.Email)
                  .HasMaxLength(255)
                  .IsRequired();

            entity.Property(u => u.PasswordHash)
                  .IsRequired();
        });

        // Resume configuration
        modelBuilder.Entity<Resume>(entity =>
        {
            entity.HasKey(r => r.Id);

            entity.Property(r => r.FileName)
                  .HasMaxLength(255)
                  .IsRequired();

            entity.Property(r => r.FilePath)
                  .HasMaxLength(500)
                  .IsRequired();

            entity.HasOne(r => r.User)
                  .WithMany()
                  .HasForeignKey(r => r.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}