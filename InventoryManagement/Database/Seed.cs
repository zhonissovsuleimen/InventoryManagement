using Bogus;
using InventoryManagement.Areas.Identity.Pages.Account;
using InventoryManagement.Data;
using InventoryManagement.Models;
using InventoryManagement.Models.Inventory;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using InventoryManagement.Models.Inventory.CustomId;
using InventoryManagement.Models.Inventory.CustomId.Element;
using System.Linq;

namespace InventoryManagement.Database
{
    public class Seed
    {
        public static async Task SeedAll(IServiceProvider services)
        {
            await SeedUsersAsync(services);
            await SeedCategoriesAsync(services);
            await SeedInventoriesAsync(services);
        }

        public static async Task SeedUsersAsync(IServiceProvider services)
        {
            const int count = 50;

            var userManager = services.GetRequiredService<UserManager<AppUser>>();

            const string adminEmail = "admin@example.com";
            const string adminPassword = "Password1!";
            var existingAdmin = await userManager.FindByEmailAsync(adminEmail);
            if (existingAdmin == null)
            {
                var adminUser = new AppUser
                {
                    FirstName = "Admin",
                    LastName = "User",
                    Email = adminEmail,
                    UserName = adminEmail,
                    EmailConfirmed = true,
                    IsAdmin = true
                };
                await userManager.CreateAsync(adminUser, adminPassword);
            }

            var faker = new Faker<RegisterModel.InputModel>()
                .RuleFor(u => u.FirstName, f => f.Name.FirstName())
                .RuleFor(u => u.LastName, f => f.Name.LastName())
                .RuleFor(u => u.Email, (f, u) => f.Internet.Email(firstName: u.FirstName, lastName: u.LastName).ToLowerInvariant())
                .RuleFor(u => u.Password, f => "Password1!");

            for (int i = 0; i < count; i++)
            {
                var input = faker.Generate();

                var existing = await userManager.FindByEmailAsync(input.Email);
                if (existing != null)
                {
                    continue;
                }

                var user = new AppUser
                {
                    FirstName = input.FirstName,
                    LastName = input.LastName,
                    Email = input.Email,
                    UserName = input.Email,
                    EmailConfirmed = true,
                    IsAdmin = false
                };

                await userManager.CreateAsync(user, input.Password);
            }
        }

        public static async Task SeedCategoriesAsync(IServiceProvider service)
        {
            var context = service.GetRequiredService<ApplicationDbContext>();
            if (await context.Categories.AnyAsync())
            {
                return;
            }

            var categories = new List<Category>
            {
                new Category { Name = "electronics" },
                new Category { Name = "books" },
                new Category { Name = "clothing" },
                new Category { Name = "home & kitchen" },
                new Category { Name = "sports" },
                new Category { Name = "music" },
                new Category { Name = "toys" }
            };

            await context.Categories.AddRangeAsync(categories);
            await context.SaveChangesAsync();
        }

        public static async Task SeedInventoriesAsync(IServiceProvider services)
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            if (await context.Inventories.AnyAsync()) return;

            var userManager = services.GetRequiredService<UserManager<AppUser>>();
            var owners = await userManager.Users.OrderBy(u => u.SequentialId).Take(5).ToListAsync();
            if (owners.Count == 0) return;

            var categories = await context.Categories.ToListAsync();
            if (categories == null || categories.Count == 0) return;

            Category Cat(string name) => categories.First(c => c.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            var existingTags = await context.Tags.ToListAsync();
            var tagMap = existingTags.ToDictionary(t => t.Name, t => t);
            Tag Tag(string nameLower)
            {
                var key = nameLower.Trim().ToLowerInvariant();
                if (!tagMap.TryGetValue(key, out var t))
                {
                    t = new Tag { Name = key };
                    context.Tags.Add(t);
                    tagMap[key] = t;
                }
                return t;
            }

            DateTime now = DateTime.UtcNow;

            // Helper to create a simple custom id pattern
            static CustomId MakeCustomId(string prefix) => new CustomId
            {
                Elements = new List<AbstractElement>
                {
                    new FixedTextElement { FixedText = prefix, Position = 1, SeparatorAfter = '-' },
                    new DateTimeElement { DateTimeFormat = "yyyy", Position = 2, SeparatorAfter = '-' },
                    new Digit6Element { Position = 3, PaddingChar = '0' }
                }
            };

            var inv1 = new Inventory
            {
                Title = "science fiction library",
                Description = "A curated shelf of classic and contemporary science fiction novels.",
                Category = Cat("books"),
                Owner = owners[0],
                IsPublic = true,
                CreatedAt = now.AddDays(-120),
                UpdatedAt = now.AddDays(-1),
                CustomId = MakeCustomId("SF"),
                SingleLine1 = new CustomField { IsUsed = true, Position = 1, Title = "author", Description = "author name" },
                MultiLine1 = new CustomField { IsUsed = true, Position = 2, Title = "synopsis", Description = "short synopsis" }
            };
            inv1.Tags.AddRange(new[] { Tag("sci-fi"), Tag("novel"), Tag("collector") });
            inv1.Items = new List<Item>
            {
                new Item { Inventory = inv1, Owner = owners[1], CustomId = inv1.GenerateCustomId(), SingleLine1 = "isaac asimov",  MultiLine1 = "Foundation trilogy boxed set", NumericLine1 = 768, BoolLine1 = true },
                new Item { Inventory = inv1, Owner = owners[2], CustomId = inv1.GenerateCustomId(), SingleLine1 = "ursula k. le guin", MultiLine1 = "The Left Hand of Darkness, 1st edition", NumericLine1 = 304, BoolLine1 = false },
                new Item { Inventory = inv1, Owner = owners[3], CustomId = inv1.GenerateCustomId(), SingleLine1 = "frank herbert", MultiLine1 = "Dune hardcover with dust jacket", NumericLine1 = 412, BoolLine1 = true },
                new Item { Inventory = inv1, Owner = owners[4 % owners.Count], CustomId = inv1.GenerateCustomId(), SingleLine1 = "liu cixin", MultiLine1 = "The Three-Body Problem paperback", NumericLine1 = 416 }
            };

            var inv2 = new Inventory
            {
                Title = "digital camera shelf",
                Description = "Consumer and prosumer cameras with accessories. All tested and working.",
                Category = Cat("electronics"),
                Owner = owners[1],
                IsPublic = true,
                CreatedAt = now.AddDays(-200),
                UpdatedAt = now.AddDays(-5),
                CustomId = MakeCustomId("CAM"),
                SingleLine1 = new CustomField { IsUsed = true, Position = 1, Title = "model", Description = "brand and model" },
                MultiLine1 = new CustomField { IsUsed = true, Position = 2, Title = "condition", Description = "cosmetic and functional notes" }
            };
            inv2.Tags.AddRange(new[] { Tag("camera"), Tag("dslr"), Tag("mirrorless"), Tag("photography") });
            inv2.Items = new List<Item>
            {
                new Item { Inventory = inv2, Owner = owners[0], CustomId = inv2.GenerateCustomId(), SingleLine1 = "canon eos 90d", MultiLine1 = "Low shutter count, 2 batteries, charger", NumericLine1 = 8200, BoolLine1 = true },
                new Item { Inventory = inv2, Owner = owners[2], CustomId = inv2.GenerateCustomId(), SingleLine1 = "sony a6400",    MultiLine1 = "Kit lens 16-50, minor scuffs on body", NumericLine1 = 12500, BoolLine1 = true },
                new Item { Inventory = inv2, Owner = owners[3], CustomId = inv2.GenerateCustomId(), SingleLine1 = "nikon d750",     MultiLine1 = "Shutter replaced last year, clean sensor", NumericLine1 = 45000, BoolLine1 = true },
                new Item { Inventory = inv2, Owner = owners[4 % owners.Count], CustomId = inv2.GenerateCustomId(), SingleLine1 = "fujifilm x-t30", MultiLine1 = "XF 18-55, recent service", NumericLine1 = 9800, BoolLine1 = true }
            };

            var inv3 = new Inventory
            {
                Title = "vintage vinyl records",
                Description = "A collection of LPs across genres and decades.",
                Category = Cat("music"),
                Owner = owners[2],
                IsPublic = true,
                CreatedAt = now.AddDays(-300),
                UpdatedAt = now.AddDays(-2),
                CustomId = MakeCustomId("VIN"),
                SingleLine1 = new CustomField { IsUsed = true, Position = 1, Title = "artist", Description = "artist or band" },
                SingleLine2 = new CustomField { IsUsed = true, Position = 2, Title = "album", Description = "album title" },
                BoolLine1 = new CustomField { IsUsed = true, Position = 3, Title = "mint", Description = "mint condition" }
            };
            inv3.Tags.AddRange(new[] { Tag("vinyl"), Tag("vintage"), Tag("analog"), Tag("collector") });
            inv3.Items = new List<Item>
            {
                new Item { Inventory = inv3, Owner = owners[0], CustomId = inv3.GenerateCustomId(), SingleLine1 = "pink floyd",  SingleLine2 = "the wall",  BoolLine1 = false },
                new Item { Inventory = inv3, Owner = owners[1], CustomId = inv3.GenerateCustomId(), SingleLine1 = "miles davis", SingleLine2 = "kind of blue", BoolLine1 = true },
                new Item { Inventory = inv3, Owner = owners[3], CustomId = inv3.GenerateCustomId(), SingleLine1 = "the beatles", SingleLine2 = "abbey road", BoolLine1 = false },
                new Item { Inventory = inv3, Owner = owners[4 % owners.Count], CustomId = inv3.GenerateCustomId(), SingleLine1 = "fleetwood mac", SingleLine2 = "rumours", BoolLine1 = true }
            };

            var inv4 = new Inventory
            {
                Title = "mountain bikes garage",
                Description = "Hardtail and full-suspension bikes for trail and enduro riding.",
                Category = Cat("sports"),
                Owner = owners[3],
                IsPublic = true,
                CreatedAt = now.AddDays(-90),
                UpdatedAt = now.AddDays(-1),
                CustomId = MakeCustomId("MTB"),
                SingleLine1 = new CustomField { IsUsed = true, Position = 1, Title = "frame", Description = "frame material and size" },
                MultiLine1 = new CustomField { IsUsed = true, Position = 2, Title = "service notes", Description = "recent maintenance" },
                NumericLine1 = new CustomField { IsUsed = true, Position = 3, Title = "mileage", Description = "estimated mileage" }
            };
            inv4.Tags.AddRange(new[] { Tag("bike"), Tag("mtb"), Tag("trail"), Tag("enduro") });
            inv4.Items = new List<Item>
            {
                new Item { Inventory = inv4, Owner = owners[1], CustomId = inv4.GenerateCustomId(), SingleLine1 = "alloy m",  MultiLine1 = "new chain and brake pads", NumericLine1 = 850,  BoolLine1 = true },
                new Item { Inventory = inv4, Owner = owners[2], CustomId = inv4.GenerateCustomId(), SingleLine1 = "carbon l", MultiLine1 = "fork serviced, tubeless setup",  NumericLine1 = 1200, BoolLine1 = true },
                new Item { Inventory = inv4, Owner = owners[0], CustomId = inv4.GenerateCustomId(), SingleLine1 = "steel s",  MultiLine1 = "fresh brake bleed",  NumericLine1 = 620, BoolLine1 = true }
            };

            var inv5 = new Inventory
            {
                Title = "kitchen essentials",
                Description = "Pots, pans, knives, and small appliances in good condition.",
                Category = Cat("home & kitchen"),
                Owner = owners[4 % owners.Count],
                IsPublic = true,
                CreatedAt = now.AddDays(-60),
                UpdatedAt = now,
                CustomId = MakeCustomId("KIT"),
                SingleLine1 = new CustomField { IsUsed = true, Position = 1, Title = "pieces", Description = "number of pieces" },
                MultiLine1 = new CustomField { IsUsed = true, Position = 2, Title = "notes", Description = "special remarks" }
            };
            inv5.Tags.AddRange(new[] { Tag("kitchen"), Tag("appliance"), Tag("cookware") });
            inv5.Items = new List<Item>
            {
                new Item { Inventory = inv5, Owner = owners[0], CustomId = inv5.GenerateCustomId(), SingleLine1 = "12", MultiLine1 = "nonstick set, silicone handles", BoolLine1 = true },
                new Item { Inventory = inv5, Owner = owners[2], CustomId = inv5.GenerateCustomId(), SingleLine1 = "6",  MultiLine1 = "stainless knives with block",  BoolLine1 = true },
                new Item { Inventory = inv5, Owner = owners[3], CustomId = inv5.GenerateCustomId(), SingleLine1 = "18", MultiLine1 = "bakeware and utensils set" }
            };

            var inv6 = new Inventory
            {
                Title = "board games collection",
                Description = "Modern strategy and co-op board games for groups.",
                Category = Cat("toys"),
                Owner = owners[0],
                IsPublic = true,
                CreatedAt = now.AddDays(-30),
                UpdatedAt = now.AddDays(-3),
                CustomId = MakeCustomId("BG"),
                SingleLine1 = new CustomField { IsUsed = true, Position = 1, Title = "condition", Description = "box and component condition" },
                MultiLine1 = new CustomField { IsUsed = true, Position = 2, Title = "notes", Description = "expansions, promos" }
            };
            inv6.Tags.AddRange(new[] { Tag("boardgame"), Tag("strategy"), Tag("co-op") });
            inv6.Items = new List<Item>
            {
                new Item { Inventory = inv6, Owner = owners[1], CustomId = inv6.GenerateCustomId(), SingleLine1 = "like new", MultiLine1 = "Wingspan + European expansion" },
                new Item { Inventory = inv6, Owner = owners[2], CustomId = inv6.GenerateCustomId(), SingleLine1 = "good",     MultiLine1 = "Gloomhaven with organizer" },
                new Item { Inventory = inv6, Owner = owners[4 % owners.Count], CustomId = inv6.GenerateCustomId(), SingleLine1 = "very good", MultiLine1 = "Terraforming Mars + Prelude" }
            };

            var inv7 = new Inventory
            {
                Title = "smartphone stock",
                Description = "Unlocked phones from recent generations with accessories.",
                Category = Cat("electronics"),
                Owner = owners[2],
                IsPublic = true,
                CreatedAt = now.AddDays(-14),
                UpdatedAt = now.AddDays(-1),
                CustomId = MakeCustomId("PHN"),
                SingleLine1 = new CustomField { IsUsed = true, Position = 1, Title = "model", Description = "device model" },
                MultiLine1 = new CustomField { IsUsed = true, Position = 2, Title = "condition", Description = "cosmetic/functional" }
            };
            inv7.Tags.AddRange(new[] { Tag("smartphone"), Tag("android"), Tag("ios") });
            inv7.Items = new List<Item>
            {
                new Item { Inventory = inv7, Owner = owners[0], CustomId = inv7.GenerateCustomId(), SingleLine1 = "iphone 13", MultiLine1 = "128GB, battery 90%, box & cable", BoolLine1 = true },
                new Item { Inventory = inv7, Owner = owners[3], CustomId = inv7.GenerateCustomId(), SingleLine1 = "samsung s22", MultiLine1 = "256GB, minor scratches, case included", BoolLine1 = true },
                new Item { Inventory = inv7, Owner = owners[1], CustomId = inv7.GenerateCustomId(), SingleLine1 = "pixel 7", MultiLine1 = "128GB, excellent condition", BoolLine1 = true }
            };

            var inv8 = new Inventory
            {
                Title = "camping gear",
                Description = "Lightweight tents and outdoor equipment.",
                Category = Cat("sports"),
                Owner = owners[1],
                IsPublic = true,
                CreatedAt = now.AddDays(-75),
                UpdatedAt = now.AddDays(-10),
                CustomId = MakeCustomId("OUT"),
                SingleLine1 = new CustomField { IsUsed = true, Position = 1, Title = "capacity", Description = "number of people" },
                MultiLine1 = new CustomField { IsUsed = true, Position = 2, Title = "features", Description = "vestibules, materials" }
            };
            inv8.Tags.AddRange(new[] { Tag("tent"), Tag("camping"), Tag("outdoor") });
            inv8.Items = new List<Item>
            {
                new Item { Inventory = inv8, Owner = owners[4 % owners.Count], CustomId = inv8.GenerateCustomId(), SingleLine1 = "2", MultiLine1 = "freestanding, aluminum poles" },
                new Item { Inventory = inv8, Owner = owners[0], CustomId = inv8.GenerateCustomId(), SingleLine1 = "4", MultiLine1 = "two doors, large vestibule" },
                new Item { Inventory = inv8, Owner = owners[2], CustomId = inv8.GenerateCustomId(), SingleLine1 = "1", MultiLine1 = "ultralight bivy" }
            };

            var inv9 = new Inventory
            {
                Title = "studio microphones",
                Description = "Condenser and dynamic mics for recording and podcasting.",
                Category = Cat("electronics"),
                Owner = owners[3],
                IsPublic = true,
                CreatedAt = now.AddDays(-180),
                UpdatedAt = now.AddDays(-7),
                CustomId = MakeCustomId("MIC"),
                SingleLine1 = new CustomField { IsUsed = true, Position = 1, Title = "model", Description = "mic model" },
                MultiLine1 = new CustomField { IsUsed = true, Position = 2, Title = "accessories", Description = "mounts, pop filters" }
            };
            inv9.Tags.AddRange(new[] { Tag("mic"), Tag("audio"), Tag("studio"), Tag("podcast") });
            inv9.Items = new List<Item>
            {
                new Item { Inventory = inv9, Owner = owners[0], CustomId = inv9.GenerateCustomId(), SingleLine1 = "shure sm7b", MultiLine1 = "boom arm, xlr cable", BoolLine1 = true },
                new Item { Inventory = inv9, Owner = owners[2], CustomId = inv9.GenerateCustomId(), SingleLine1 = "rode nt1-a",  MultiLine1 = "shock mount, pop filter", BoolLine1 = true }
            };

            var inv10 = new Inventory
            {
                Title = "designer jackets",
                Description = "Seasonal outerwear, lightly used.",
                Category = Cat("clothing"),
                Owner = owners[4 % owners.Count],
                IsPublic = true,
                CreatedAt = now.AddDays(-22),
                UpdatedAt = now.AddDays(-2),
                CustomId = MakeCustomId("JKT"),
                SingleLine1 = new CustomField { IsUsed = true, Position = 1, Title = "size", Description = "jacket size" },
                SingleLine2 = new CustomField { IsUsed = true, Position = 2, Title = "designer", Description = "brand" }
            };
            inv10.Tags.AddRange(new[] { Tag("jacket"), Tag("fashion") });
            inv10.Items = new List<Item>
            {
                new Item { Inventory = inv10, Owner = owners[1], CustomId = inv10.GenerateCustomId(), SingleLine1 = "m", SingleLine2 = "patagonia" },
                new Item { Inventory = inv10, Owner = owners[2], CustomId = inv10.GenerateCustomId(), SingleLine1 = "l", SingleLine2 = "north face" }
            };

            var inventories = new List<Inventory> { inv1, inv2, inv3, inv4, inv5, inv6, inv7, inv8, inv9, inv10 };

            await context.Inventories.AddRangeAsync(inventories);
            await context.SaveChangesAsync();
        }
    }
}
