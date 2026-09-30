using Microsoft.EntityFrameworkCore;

namespace Login_form.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : DbContext(options)
{
    public DbSet<UserAccount> Users => Set<UserAccount>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.ToTable("AspNetUsers", "dbo");
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Id).HasMaxLength(450);
            entity.Property(user => user.Email).HasMaxLength(256);
            entity.Property(user => user.NormalizedEmail).HasMaxLength(256);
            entity.Property(user => user.PasswordHash);
        });
    }
}
