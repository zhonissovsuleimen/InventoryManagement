using InventoryManagement.Models;
using InventoryManagement.Models.Inventory;
using InventoryManagement.Models.Inventory.CustomId;
using InventoryManagement.Models.Inventory.CustomId.Element;
using InventoryManagement.Models.Discussion;
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
            ConfigureItems(builder);
            ConfigureDiscussion(builder);

            ConfigureCustomId(builder);
            ConfigureCustomIdElement(builder);

            ConfigureTags(builder);
        }

        public DbSet<Category> Categories { get; set; } = default!;
        public DbSet<Inventory> Inventories { get; set; } = default!;
        public DbSet<CustomId> CustomIds { get; set; } = default!;
        public DbSet<Item> Items { get; set; } = default!;
        public DbSet<DiscussionPost> DiscussionPosts { get; set; } = default!;
        public DbSet<Tag> Tags { get; set; } = default!;


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

        private void ConfigureItems(ModelBuilder builder)
        {
            builder.Entity<Item>(entity =>
            {
                entity.HasOne(e => e.Inventory)
                      .WithMany(i => i.Items)
                      .HasForeignKey("InventoryId")
                      .HasConstraintName("FK_Item_Inventory")
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Owner)
                      .WithMany()
                      .HasForeignKey("Owner_UserId")
                      .HasConstraintName("FK_Item_Owner")
                      .OnDelete(DeleteBehavior.SetNull);
            });
        }

        private void ConfigureDiscussion(ModelBuilder builder)
        {
            builder.Entity<DiscussionPost>(entity =>
            {
                entity.ToTable("DiscussionPosts");
                entity.HasKey(p => p.Id);
                entity.HasIndex(p => new { p.InventoryId, p.CreatedAtUtc });
                entity.HasOne(p => p.Inventory)
                      .WithMany()
                      .HasForeignKey(p => p.InventoryId)
                      .HasConstraintName("FK_DiscussionPost_Inventory")
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(p => p.User)
                      .WithMany()
                      .HasForeignKey(p => p.UserId)
                      .HasConstraintName("FK_DiscussionPost_User")
                      .OnDelete(DeleteBehavior.Cascade);
                entity.Property(p => p.ContentMarkdown).HasMaxLength(4000).IsRequired();
            });
        }

        private void ConfigureCustomId(ModelBuilder builder)
        {
            builder.Entity<CustomId>()
                .HasMany<AbstractElement>("Elements")
                .WithOne(e => e.CustomId)
                .HasForeignKey("CustomId_Id");
        }

        private void ConfigureCustomIdElement(ModelBuilder builder)
        {
            builder.Entity<AbstractElement>()
                .HasDiscriminator<string>("ElementType")
                .HasValue<FixedTextElement>("FixedText")
                .HasValue<Bit20Element>("Bit20")
                .HasValue<Bit32Element>("Bit32")
                .HasValue<Digit6Element>("Digit6")
                .HasValue<Digit9Element>("Digit9")
                .HasValue<GuidElement>("Guid")
                .HasValue<DateTimeElement>("DateTime");
            //.HasValue<SequentialElement>("Sequential")
        }

        private void ConfigureTags(ModelBuilder builder)
        {
            builder.Entity<Tag>(entity =>
            {
                entity.ToTable("Tags");
                entity.HasKey(t => t.Id);
                entity.HasIndex(t => t.Name).IsUnique();
                entity.Property(t => t.Name).HasMaxLength(64).IsRequired();
            });

            builder.Entity<Inventory>()
                .HasMany(i => i.Tags)
                .WithMany(t => t.Inventories)
                .UsingEntity<Dictionary<string, object>>(
                    "InventoryTags",
                    right => right
                        .HasOne<Tag>()
                        .WithMany()
                        .HasForeignKey("TagId")
                        .HasConstraintName("FK_InventoryTags_TagId")
                        .OnDelete(DeleteBehavior.Cascade),
                    left => left
                        .HasOne<Inventory>()
                        .WithMany()
                        .HasForeignKey("InventoryId")
                        .HasConstraintName("FK_InventoryTags_InventoryId")
                        .OnDelete(DeleteBehavior.Cascade),
                    join =>
                    {
                        join.ToTable("InventoryTags");
                        join.HasKey("InventoryId", "TagId");
                        join.HasIndex("TagId");
                        // Index to accelerate Inventory lookup by Tag
                        join.HasIndex("InventoryId");
                    });
        }
    }
}
