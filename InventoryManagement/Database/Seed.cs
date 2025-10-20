using Bogus;
using InventoryManagement.Areas.Identity.Pages.Account;
using InventoryManagement.Data;
using InventoryManagement.Models;
using InventoryManagement.Models.Inventory;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InventoryManagement.Database
{
    public class Seed
    {
        public static async Task SeedAll(IServiceProvider services)
        {
            await SeedUsersAsync(services);
            await SeedCategoriesAsync(services);
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
    }
}
