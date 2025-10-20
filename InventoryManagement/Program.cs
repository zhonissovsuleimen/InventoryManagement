using InventoryManagement.Data;
using InventoryManagement.Database;
using InventoryManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            // Add services to the container.
            var connectionString = @"Host=localhost;Username=project-user;Password=password123;Database=project";
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString));
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddDefaultIdentity<AppUser>()
                .AddEntityFrameworkStores<ApplicationDbContext>();
            builder.Services.AddRazorPages();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
                // dotnet run seed
                if (args.Length > 0 && args[0].Equals("seed", StringComparison.OrdinalIgnoreCase))
                {
                    using var scope = app.Services.CreateScope();
                    Console.WriteLine("Seeding data");
                    await Seed.SeedAll(scope.ServiceProvider);
                    Console.WriteLine("Seeding data done");
                    return;
                }
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapRazorPages()
               .WithStaticAssets();

            app.Run();
        }
    }
}
