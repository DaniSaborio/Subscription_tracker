using Microsoft.EntityFrameworkCore;
using Subscription_tracker.API.Models;

namespace Subscription_tracker.API.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<Subscription> Subscriptions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>()
            .HasKey(u => u.Id);

        modelBuilder.Entity<User>()
            .Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(255);

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // Subscription configuration
        modelBuilder.Entity<Subscription>()
            .HasKey(s => s.Id);

        modelBuilder.Entity<Subscription>()
            .HasOne(s => s.User)
            .WithMany(u => u.Subscriptions)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Subscription>()
            .Property(s => s.Amount)
            .HasPrecision(10, 2);

        modelBuilder.Entity<Subscription>()
            .Property(s => s.ServiceName)
            .IsRequired()
            .HasMaxLength(255);

        modelBuilder.Entity<Subscription>()
            .Property(s => s.Category)
            .IsRequired()
            .HasMaxLength(100);

        modelBuilder.Entity<Subscription>()
            .Property(s => s.BillingCycle)
            .IsRequired()
            .HasMaxLength(50);
    }
}
