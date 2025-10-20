using InventoryManagement.Models;
using InventoryManagement.Models.Inventory;
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
            ConfigureInventory(builder);
            ConfigureUserInventory(builder);
        }

        public DbSet<Category> Categories { get; set; } = default!;
        public DbSet<Inventory> Inventories { get; set; } = default!;

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

        private void ConfigureInventory(ModelBuilder builder)
        {
            builder.Entity<Inventory>()
                .Property(i => i.SearchVector)
                .HasComputedColumnSql(
                    "to_tsvector('simple', coalesce(\"Title\", ''))",
                    stored: true);


            builder.Entity<Inventory>()
                .HasOne(i => i.Category)
                .WithMany(c => c.Inventories);

            builder.Entity<Inventory>().OwnsOne(i => i.SingleLine1);
            builder.Entity<Inventory>().OwnsOne(i => i.SingleLine2);
            builder.Entity<Inventory>().OwnsOne(i => i.SingleLine3);
            builder.Entity<Inventory>().OwnsOne(i => i.MultiLine1);
            builder.Entity<Inventory>().OwnsOne(i => i.MultiLine2);
            builder.Entity<Inventory>().OwnsOne(i => i.MultiLine3);
            builder.Entity<Inventory>().OwnsOne(i => i.NumericLine1);
            builder.Entity<Inventory>().OwnsOne(i => i.NumericLine2);
            builder.Entity<Inventory>().OwnsOne(i => i.NumericLine3);
            builder.Entity<Inventory>().OwnsOne(i => i.BoolLine1);
            builder.Entity<Inventory>().OwnsOne(i => i.BoolLine2);
            builder.Entity<Inventory>().OwnsOne(i => i.BoolLine3);
        }

        private void ConfigureUserInventory(ModelBuilder builder)
        {
            builder.Entity<Inventory>()
                .HasOne(i => i.Owner)
                .WithMany(u => u.OwnedInventories)
                .HasForeignKey("Owner_UserId")
                .HasConstraintName("FK_Inventory_Owner");

            builder.Entity<Inventory>()
                .HasMany(i => i.AllowedUsers)
                .WithMany(u => u.AllowedInventories)
                .UsingEntity<Dictionary<string, object>>(
                    "InventoryAllowedUsers",
                    right => right
                        .HasOne<AppUser>()
                        .WithMany()
                        .HasForeignKey("UserId")
                        .HasConstraintName("FK_InventoryAllowedUsers_UserId")
                        .OnDelete(DeleteBehavior.Cascade),
                    left => left
                        .HasOne<Inventory>()
                        .WithMany()
                        .HasForeignKey("InventoryId")
                        .HasConstraintName("FK_InventoryAllowedUsers_InventoryId")
                        .OnDelete(DeleteBehavior.Cascade),
                    join =>
                    {
                        join.ToTable("InventoryAllowedUsers");
                        join.HasKey("InventoryId", "UserId");
                        join.HasIndex("UserId");
                    });
        }
    }
}
