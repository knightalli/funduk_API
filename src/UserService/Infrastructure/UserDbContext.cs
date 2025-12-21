using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using UserService.Domain;

namespace UserService.Infrastructure;

public class UserDbContext(DbContextOptions<UserDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<User> UserProfiles => Set<User>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<User>(cfg =>
        {
            cfg.ToTable("UserProfiles");
            cfg.HasKey(u => u.Id);
            cfg.Property(u => u.Id)
                .HasConversion(id => id.Value, value => new UserId(value));

            cfg.Property(u => u.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            cfg.Property(u => u.LastName)
                .IsRequired()
                .HasMaxLength(100);

            cfg.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(256);
        });
    }
}