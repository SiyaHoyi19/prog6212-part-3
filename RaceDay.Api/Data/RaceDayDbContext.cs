using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Models;

namespace RaceDay.Api.Data;

public class RaceDayDbContext(DbContextOptions<RaceDayDbContext> options) : DbContext(options)
{
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Enrolment> Enrolments => Set<Enrolment>();
    public DbSet<Result> Results => Set<Result>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
            entity.HasKey(x => x.RoleId);
            entity.Property(x => x.RoleId).ValueGeneratedOnAdd();
            entity.Property(x => x.Name).HasMaxLength(30).IsRequired();
            entity.HasIndex(x => x.Name).IsUnique();
            entity.HasData(
                new Role { RoleId = 1, Name = "Organiser" },
                new Role { RoleId = 2, Name = "Participant" });
        });

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.ToTable("AppUsers");
            entity.HasKey(x => x.UserId);
            entity.Property(x => x.UserId).ValueGeneratedOnAdd();
            entity.Property(x => x.FullName).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(50).IsRequired();
            entity.Property(x => x.PhoneNumber).HasMaxLength(15).IsRequired();
            entity.Property(x => x.Username).HasMaxLength(50).IsRequired();
            entity.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
            entity.Property(x => x.ProfileImageUrl).HasMaxLength(2048);
            entity.HasIndex(x => x.Email).IsUnique();
            entity.HasIndex(x => x.Username).IsUnique();
            entity.HasOne(x => x.Role).WithMany(x => x.Users)
                .HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Event>(entity =>
        {
            entity.ToTable("Events");
            entity.HasKey(x => x.EventId);
            entity.Property(x => x.EventId).ValueGeneratedOnAdd();
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Location).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Type).HasMaxLength(10).IsRequired();
            entity.Property(x => x.BannerImageUrl).HasMaxLength(2048);
            entity.HasOne(x => x.Organiser).WithMany(x => x.Events)
                .HasForeignKey(x => x.OrganiserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("Categories");
            entity.HasKey(x => x.CategoryId);
            entity.Property(x => x.CategoryId).ValueGeneratedOnAdd();
            entity.HasOne(x => x.Event).WithMany(x => x.Categories)
                .HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Enrolment>(entity =>
        {
            entity.ToTable("Enrolments");
            entity.HasKey(x => x.EnrolmentId);
            entity.Property(x => x.EnrolmentId).ValueGeneratedOnAdd();
            entity.HasIndex(x => new { x.UserId, x.EventId }).IsUnique();
            entity.HasOne(x => x.User).WithMany(x => x.Enrolments)
                .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Event).WithMany(x => x.Enrolments)
                .HasForeignKey(x => x.EventId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Category).WithMany(x => x.Enrolments)
                .HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Result).WithOne(x => x.Enrolment)
                .HasForeignKey<Result>(x => x.EnrolmentId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Result>(entity =>
        {
            entity.ToTable("Results");
            entity.HasKey(x => x.ResultId);
            entity.Property(x => x.ResultId).ValueGeneratedOnAdd();
            entity.HasIndex(x => x.EnrolmentId).IsUnique();
        });
    }
}
