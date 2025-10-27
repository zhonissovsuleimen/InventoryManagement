using Bogus;
using InventoryManagement.Areas.Identity.Pages.Account;
using InventoryManagement.Data;
using InventoryManagement.Models;
using InventoryManagement.Models.Inventory;
using InventoryManagement.Models.CustomId.Element;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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
            const int inventoryCount = 30;
            const int maxAllowedUsers = 4;
            const int maxCustomIdElements = 4;
            const int customIdElementTypeCount = 7;

            const float separatorProb = 0.3f;
            const float fieldUsedProb = 0.5f;
            const float publicProb = 0.3f;

            var context = services.GetRequiredService<ApplicationDbContext>();            
            var userManager = services.GetRequiredService<UserManager<AppUser>>();
            var users = await userManager.Users.ToListAsync();
            if (users == null || users.Count == 0) return;
            
            var categories = await context.Categories.ToListAsync();
            if (categories == null || categories.Count == 0) return;

            var faker = new Faker();
            var inventories = new List<Inventory>();
            var rng = new Random();

            var separators = new char?[] { null, '-', '_', '/', '.' };

            for (int i = 0; i < inventoryCount; i++)
            {
                var title = faker.Commerce.ProductName();
                var description = faker.Lorem.Paragraphs(1);
                var category = faker.PickRandom(categories);
                var owner = faker.PickRandom(users);

                var allowedMax = Math.Min(maxAllowedUsers, Math.Max(0, users.Count - 1));
                var allowedCount = faker.Random.Int(0, allowedMax);
                var allowed = new List<AppUser>();
                if (allowedCount > 0)
                {
                    var pool = users.Where(u => u.Id != owner.Id).ToList();
                    allowed = faker.PickRandom(pool, allowedCount).ToList();
                }

                var createdAt = DateTime.UtcNow.AddDays(-faker.Random.Int(0, 365));
                var updatedAt = createdAt.AddDays(faker.Random.Int(0, Math.Max(0, (DateTime.UtcNow - createdAt).Days)));

                var customId = new InventoryManagement.Models.CustomId.CustomId();
                int elementCount = faker.Random.Int(1, Math.Max(1, maxCustomIdElements)); // ensure at least one element
                for (int e = 0; e < elementCount; e++)
                {
                    var pick = faker.Random.Int(0, customIdElementTypeCount - 1);
                    AbstractElement element = pick switch
                    {
                        0 => new FixedTextElement { FixedText = faker.Commerce.Department().ToUpperInvariant() },
                        1 => new GuidElement(),
                        2 => new Digit6Element { Radix = faker.Random.Bool() ? Radix.Decimal : Radix.Hexadecimal, PaddingChar = '0' },
                        3 => new Digit9Element { Radix = Radix.Decimal, PaddingChar = '0' },
                        4 => new Bit20Element { Radix = Radix.Hexadecimal, PaddingChar = '0' },
                        5 => new Bit32Element { Radix = Radix.Hexadecimal, PaddingChar = '0' },
                        _ => new DateTimeElement { DateTimeFormat = faker.Random.ArrayElement(new[] { "yyyy", "yyyyMM", "yyyyMMdd" }) },
                    };
                    element.SeparatorBefore = faker.Random.Bool(separatorProb) ? faker.PickRandom(separators) : null;
                    element.SeparatorAfter = faker.Random.Bool(separatorProb) ? faker.PickRandom(separators) : null;
                    element.CustomId = customId;
                    element.Value = element.Generate(rng);
                    customId.Elements.Add(element);
                }

                CustomField MakeField()
                {
                    var used = faker.Random.Bool(fieldUsedProb);
                    var cf = new CustomField
                    {
                        IsUsed = used,
                        Position = (short?)faker.Random.Int(1, 10)
                    };

                    if (cf.IsUsed)
                    {
                        cf.Title = faker.Commerce.ProductAdjective();
                        cf.Description = faker.Lorem.Sentence();
                    }

                    return cf;
                }

                var single1 = MakeField();
                var single2 = MakeField();
                var single3 = MakeField();
                var multi1 = MakeField();
                var multi2 = MakeField();
                var multi3 = MakeField();
                var numeric1 = MakeField();
                var numeric2 = MakeField();
                var numeric3 = MakeField();
                var bool1 = MakeField();
                var bool2 = MakeField();
                var bool3 = MakeField();

                var allFields = new List<CustomField> { single1, single2, single3, multi1, multi2, multi3, numeric1, numeric2, numeric3, bool1, bool2, bool3 };
                if (!allFields.Any(f => f.IsUsed))
                {
                    var pickIndex = faker.Random.Int(0, allFields.Count - 1);
                    var forced = allFields[pickIndex];
                    forced.IsUsed = true;
                    forced.Title = faker.Commerce.ProductAdjective();
                    forced.Description = faker.Lorem.Sentence();
                }

                var inv = new Inventory
                {
                    Title = title,
                    Description = description,
                    Category = category,
                    Owner = owner,
                    AllowedUsers = allowed,
                    IsPublic = faker.Random.Bool(publicProb),
                    ImageUrl = faker.Internet.Avatar(),
                    CreatedAt = createdAt,
                    UpdatedAt = updatedAt,
                    CustomId = customId,

                    SingleLine1 = single1,
                    SingleLine2 = single2,
                    SingleLine3 = single3,
                    MultiLine1 = multi1,
                    MultiLine2 = multi2,
                    MultiLine3 = multi3,
                    NumericLine1 = numeric1,
                    NumericLine2 = numeric2,
                    NumericLine3 = numeric3,
                    BoolLine1 = bool1,
                    BoolLine2 = bool2,
                    BoolLine3 = bool3
                };

                inventories.Add(inv);
            }

            await context.Inventories.AddRangeAsync(inventories);
            await context.SaveChangesAsync();
        }
    }
}
