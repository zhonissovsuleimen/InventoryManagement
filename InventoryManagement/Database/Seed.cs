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
            const int count = 100;

            var userManager = services.GetRequiredService<UserManager<AppUser>>();

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
                    EmailConfirmed = false,
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
                new Category { Name = "Electronics" },
                new Category { Name = "Books" },
                new Category { Name = "Clothing" },
                new Category { Name = "Home & Kitchen" },
                new Category { Name = "Sports" }
            };

            await context.Categories.AddRangeAsync(categories);
            await context.SaveChangesAsync();
        }

        public static async Task SeedInventoriesAsync(IServiceProvider services)
        {
            var context = services.GetRequiredService<ApplicationDbContext>();
            if (await context.Inventories.AnyAsync()) return;

            var userManager = services.GetRequiredService<UserManager<AppUser>>();
            var owner = await userManager.Users.FirstOrDefaultAsync();
            if (owner == null) return;

            var categories = await context.Categories.ToListAsync();
            if (categories == null || categories.Count == 0) return;

            var now = DateTime.UtcNow;
            var rng = new Random();

            var inventories = new List<Inventory>
            {
                new()
                {
                    Title = "Science Fiction Books",
                    Description = "A curated collection of classic and contemporary science fiction novels.",
                    Category = categories[rng.Next(0, categories.Count)],
                    Owner = owner,
                    AllowedUsers = [],
                    IsPublic = true,
                    ImageUrl = null,
                    CreatedAt = now.AddDays(-30),
                    UpdatedAt = now,
                    CustomId = new CustomId
                    {
                        Elements = new List<AbstractElement>
                        {
                            new FixedTextElement { FixedText = "SF", Position = 1, SeparatorAfter = '-' },
                            new DateTimeElement { DateTimeFormat = "yyyy", Position = 2, SeparatorAfter = '-' },
                            new Digit6Element { Position = 3, PaddingChar = '0' }
                        }
                    },
                    SingleLine1 = new CustomField { IsUsed = true, Position = 1, Title = "Author", Description = "Author name" },
                    MultiLine1 = new CustomField { IsUsed = true, Position = 2, Title = "Synopsis", Description = "Short synopsis" },
                    NumericLine1 = new CustomField { IsUsed = true, Position = 3, Title = "Pages", Description = "Number of pages" },
                    BoolLine1 = new CustomField { IsUsed = true, Position = 4, Title = "Signed", Description = "Signed by author" }
                },

                new()
                {
                    Title = "Digital Cameras",
                    Description = "Consumer and prosumer cameras and accessories.",
                    Category = categories[rng.Next(0, categories.Count)],
                    Owner = owner,
                    AllowedUsers = [],
                    IsPublic = true,
                    CreatedAt = now.AddDays(-90),
                    UpdatedAt = now,
                    CustomId = new CustomId
                    {
                        Elements = new List<AbstractElement>
                        {
                            new FixedTextElement { FixedText = "CAM", Position = 1, SeparatorAfter = '-' },
                            new GuidElement { Position = 2 }
                        }
                    },
                    SingleLine1 = new CustomField { IsUsed = true, Position = 1, Title = "Brand/Model", Description = "Brand and model" },
                    MultiLine1 = new CustomField { IsUsed = true, Position = 2, Title = "Specs", Description = "Technical specifications" }
                },

                new()
                {
                    Title = "Vintage Vinyl",
                    Description = "A collection of vinyl records across genres and decades.",
                    Category = categories[rng.Next(0, categories.Count)],
                    Owner = owner,
                    AllowedUsers = [],
                    IsPublic = true,
                    CreatedAt = now.AddDays(-200),
                    UpdatedAt = now,
                    CustomId = new CustomId { Elements = new List<AbstractElement> { new FixedTextElement { FixedText = "VIN", Position = 1 }, new Digit6Element { Position = 2 } } },
                    SingleLine1 = new CustomField { IsUsed = true, Position = 1, Title = "Artist", Description = "Artist or band" },
                    SingleLine2 = new CustomField { IsUsed = true, Position = 2, Title = "Album", Description = "Album title" },
                    BoolLine1 = new CustomField { IsUsed = true, Position = 3, Title = "Mint", Description = "Mint condition" }
                },

                new()
                {
                    Title = "Board Games",
                    Description = "Modern board games and strategy games for groups.",
                    Category = categories[rng.Next(0, categories.Count)],
                    Owner = owner,
                    AllowedUsers = [],
                    IsPublic = true,
                    CreatedAt = now.AddDays(-45),
                    UpdatedAt = now,
                    CustomId = new CustomId { Elements = new List<AbstractElement> { new FixedTextElement { FixedText = "BG", Position = 1 }, new Digit6Element { Position = 2 } } },
                    SingleLine1 = new CustomField { IsUsed = true, Position = 1, Title = "Condition", Description = "Condition of the item" },
                    MultiLine1 = new CustomField { IsUsed = true, Position = 2, Title = "Notes", Description = "Additional notes" }
                },

                new()
                {
                    Title = "Home Appliances",
                    Description = "Small kitchen and home appliances in good condition.",
                    Category = categories[rng.Next(0, categories.Count)],
                    Owner = owner,
                    AllowedUsers = [],
                    IsPublic = true,
                    CreatedAt = now.AddDays(-120),
                    UpdatedAt = now,
                    CustomId = new CustomId { Elements = new List<AbstractElement> { new FixedTextElement { FixedText = "HA", Position = 1 }, new Digit9Element { Position = 2 } } },
                    SingleLine1 = new CustomField { IsUsed = true, Position = 1, Title = "Model", Description = "Model number" },
                    MultiLine1 = new CustomField { IsUsed = true, Position = 2, Title = "Specs", Description = "Specifications" }
                },

                new()
                {
                    Title = "Mountain Bikes",
                    Description = "Hardtail and full-suspension bikes for trail and enduro riding. Many include upgraded drivetrains and recent service history.",
                    Category = categories[rng.Next(0, categories.Count)],
                    Owner = owner,
                    AllowedUsers = [],
                    IsPublic = true,
                    CreatedAt = now.AddDays(-60),
                    UpdatedAt = now,
                    CustomId = new CustomId { Elements = new List<AbstractElement> { new FixedTextElement { FixedText = "MTB", Position = 1 }, new DateTimeElement { DateTimeFormat = "yy", Position = 2 }, new Digit6Element { Position = 3 } } },
                    SingleLine1 = new CustomField { IsUsed = true, Position = 1, Title = "Frame", Description = "Frame material and size" },
                    MultiLine1 = new CustomField { IsUsed = true, Position = 2, Title = "Service Notes", Description = "Recent maintenance and upgrades" }
                },

                new()
                {
                    Title = "Running Shoes",
                    Description = "Road and trail running shoes in various sizes; performance models with light wear.",
                    Category = categories[rng.Next(0, categories.Count)],
                    Owner = owner,
                    AllowedUsers = [],
                    IsPublic = true,
                    CreatedAt = now.AddDays(-15),
                    UpdatedAt = now,
                    CustomId = new CustomId { Elements = new List<AbstractElement> { new FixedTextElement { FixedText = "RUN", Position = 1 }, new Digit6Element { Position = 2 } } },
                    SingleLine1 = new CustomField { IsUsed = true, Position = 1, Title = "Size", Description = "Shoe size" },
                    SingleLine2 = new CustomField { IsUsed = true, Position = 2, Title = "Brand", Description = "Brand name" }
                },

                new()
                {
                    Title = "Smartphones",
                    Description = "Unlocked phones from recent generations, some with accessories and spare batteries.",
                    Category = categories[rng.Next(0, categories.Count)],
                    Owner = owner,
                    AllowedUsers = [],
                    IsPublic = true,
                    CreatedAt = now.AddDays(-10),
                    UpdatedAt = now,
                    CustomId = new CustomId { Elements = new List<AbstractElement> { new FixedTextElement { FixedText = "PHN", Position = 1 }, new GuidElement { Position = 2 } } },
                    SingleLine1 = new CustomField { IsUsed = true, Position = 1, Title = "Model", Description = "Model identifier" },
                    MultiLine1 = new CustomField { IsUsed = true, Position = 2, Title = "Condition", Description = "Cosmetic and functional condition" }
                },

                new()
                {
                    Title = "Kitchenware Set",
                    Description = "Mixed set: pans, knives, utensils and a few small appliances; all thoroughly cleaned and functional.",
                    Category = categories[rng.Next(0, categories.Count)],
                    Owner = owner,
                    AllowedUsers = [],
                    IsPublic = true,
                    CreatedAt = now.AddDays(-75),
                    UpdatedAt = now,
                    CustomId = new CustomId { Elements = new List<AbstractElement> { new FixedTextElement { FixedText = "KWT", Position = 1 }, new Digit9Element { Position = 2 } } },
                    SingleLine1 = new CustomField { IsUsed = true, Position = 1, Title = "Pieces", Description = "Number of items in set" },
                    MultiLine1 = new CustomField { IsUsed = true, Position = 2, Title = "Notes", Description = "Special remarks" }
                },

                new()
                {
                    Title = "Graphic Novels",
                    Description = "Collector and contemporary graphic novels; many first prints and signed copies.",
                    Category = categories[rng.Next(0, categories.Count)],
                    Owner = owner,
                    AllowedUsers = [],
                    IsPublic = true,
                    CreatedAt = now.AddDays(-5),
                    UpdatedAt = now,
                    CustomId = new CustomId { Elements = new List<AbstractElement> { new FixedTextElement { FixedText = "GN", Position = 1 }, new DateTimeElement { DateTimeFormat = "yyyy", Position = 2 }, new Digit6Element { Position = 3 } } },
                    SingleLine1 = new CustomField { IsUsed = true, Position = 1, Title = "Author/Artist", Description = "Primary creator(s)" },
                    MultiLine1 = new CustomField { IsUsed = true, Position = 2, Title = "Edition Notes", Description = "Edition and signing information" }
                },

                new()
                {
                    Title = "Studio Microphones",
                    Description = "Condenser and dynamic microphones, some with shock mounts and pop filters; tested and ready for recording.",
                    Category = categories[rng.Next(0, categories.Count)],
                    Owner = owner,
                    AllowedUsers = [],
                    IsPublic = true,
                    CreatedAt = now.AddDays(-300),
                    UpdatedAt = now,
                    CustomId = new CustomId { Elements = new List<AbstractElement> { new FixedTextElement { FixedText = "MIC", Position = 1 }, new Digit6Element { Position = 2 } } },
                    SingleLine1 = new CustomField { IsUsed = true, Position = 1, Title = "Model", Description = "Microphone model" },
                    MultiLine1 = new CustomField { IsUsed = true, Position = 2, Title = "Accessorries", Description = "Included accessories" }
                },

                new()
                {
                    Title = "Vintage Cameras",
                    Description = "Film cameras from mid-20th century; many are rangefinders with unique character.",
                    Category = categories[rng.Next(0, categories.Count)],
                    Owner = owner,
                    AllowedUsers = [],
                    IsPublic = true,
                    CreatedAt = now.AddYears(-2),
                    UpdatedAt = now,
                    CustomId = new CustomId { Elements = new List<AbstractElement> { new FixedTextElement { FixedText = "VTC", Position = 1 }, new Digit6Element { Position = 2 } } },
                    SingleLine1 = new CustomField { IsUsed = true, Position = 1, Title = "Make/Model", Description = "Manufacturer and model" },
                    MultiLine1 = new CustomField { IsUsed = true, Position = 2, Title = "Condition", Description = "Cosmetic and mechanical condition" }
                },

                new()
                {
                    Title = "Camping Tents",
                    Description = "Lightweight 2–6 person tents; some freestanding, some with vestibules and gear storage.",
                    Category = categories[rng.Next(0, categories.Count)],
                    Owner = owner,
                    AllowedUsers = [],
                    IsPublic = true,
                    CreatedAt = now.AddDays(-140),
                    UpdatedAt = now,
                    CustomId = new CustomId { Elements = new List<AbstractElement> { new FixedTextElement { FixedText = "TNT", Position = 1 }, new Digit9Element { Position = 2 } } },
                    SingleLine1 = new CustomField { IsUsed = true, Position = 1, Title = "Capacity", Description = "Number of people" },
                    MultiLine1 = new CustomField { IsUsed = true, Position = 2, Title = "Features", Description = "Doors, vestibules, materials" }
                },

                new()
                {
                    Title = "Designer Jackets",
                    Description = "Seasonal outerwear from contemporary designers; sizes labeled and lightly used.",
                    Category = categories[rng.Next(0, categories.Count)],
                    Owner = owner,
                    AllowedUsers = [],
                    IsPublic = true,
                    CreatedAt = now.AddDays(-20),
                    UpdatedAt = now,
                    CustomId = new CustomId { Elements = new List<AbstractElement> { new FixedTextElement { FixedText = "DJ", Position = 1 }, new Digit6Element { Position = 2 } } },
                    SingleLine1 = new CustomField { IsUsed = true, Position = 1, Title = "Size", Description = "Jacket size" },
                    SingleLine2 = new CustomField { IsUsed = true, Position = 2, Title = "Designer", Description = "Designer or brand" }
                },

                new()
                {
                    Title = "Board Game Expansions",
                    Description = "Expansion packs and promo cards for popular board games; includes rare print promos.",
                    Category = categories[rng.Next(0, categories.Count)],
                    Owner = owner,
                    AllowedUsers = [],
                    IsPublic = true,
                    CreatedAt = now.AddDays(-8),
                    UpdatedAt = now,
                    CustomId = new CustomId { Elements = new List<AbstractElement> { new FixedTextElement { FixedText = "BGE", Position = 1 }, new Digit6Element { Position = 2 } } },
                    SingleLine1 = new CustomField { IsUsed = true, Position = 1, Title = "Base Game", Description = "Base game required" },
                    MultiLine1 = new CustomField { IsUsed = true, Position = 2, Title = "Contents", Description = "What the expansion includes" }
                },

                new()
                {
                    Title = "Art Supplies",
                    Description = "Canvas, paints, brushes and mixed-media materials; ideal for hobbyists and small studios.",
                    Category = categories[rng.Next(0, categories.Count)],
                    Owner = owner,
                    AllowedUsers = [],
                    IsPublic = true,
                    CreatedAt = now.AddDays(-2),
                    UpdatedAt = now,
                    CustomId = new CustomId { Elements = new List<AbstractElement> { new FixedTextElement { FixedText = "ART", Position = 1 }, new Digit6Element { Position = 2 } } },
                    SingleLine1 = new CustomField { IsUsed = true, Position = 1, Title = "Type", Description = "Paints, brushes, canvas etc." },
                    MultiLine1 = new CustomField { IsUsed = true, Position = 2, Title = "Condition", Description = "New, opened, used" }
                }
            };

            await context.Inventories.AddRangeAsync(inventories);
            await context.SaveChangesAsync();
        }
    }
}
