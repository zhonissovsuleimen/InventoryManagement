using InventoryManagement.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Data
{
    public class ApplicationDbContext : IdentityDbContext<AppUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) {}

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            ConfigureUsers(builder);
        }

        private void ConfigureUsers(ModelBuilder builder)
        {
            builder.HasPostgresExtension("pg_trgm");

            builder.Entity<AppUser>()
                .HasIndex(u => u.SearchVector)
                .HasMethod("GIN");

            builder.Entity<AppUser>()
                .HasIndex(u => u.FirstName)
                .HasMethod("GIN")
                .HasOperators("gin_trgm_ops");

            builder.Entity<AppUser>()
                .HasIndex(u => u.LastName)
                .HasMethod("GIN")
                .HasOperators("gin_trgm_ops");

            builder.Entity<AppUser>()
                .Property(u => u.SequentialId)
                .ValueGeneratedOnAdd();

            builder.Entity<AppUser>()
                .Property(u => u.SearchVector)
                .HasComputedColumnSql(
                    "to_tsvector('simple', coalesce(\"FirstName\", '') || ' ' || coalesce(\"LastName\", ''))",
                    stored: true);

            builder.Entity<AppUser>().ToTable("Users");

            builder.Entity<AppUser>().HasIndex(u => u.NormalizedEmail).IsUnique();
            builder.Entity<AppUser>().Ignore(u => u.PhoneNumber);
            builder.Entity<AppUser>().Ignore(u => u.PhoneNumberConfirmed);
            builder.Entity<AppUser>().Ignore(u => u.TwoFactorEnabled);
            builder.Entity<AppUser>().Ignore(u => u.AccessFailedCount);
            builder.Entity<AppUser>().Ignore(u => u.LockoutEnabled);
            builder.Entity<AppUser>().Ignore(u => u.LockoutEnd);
        }
    }
}
