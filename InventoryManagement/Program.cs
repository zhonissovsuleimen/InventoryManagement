using InventoryManagement.Data;
using InventoryManagement.Database;
using InventoryManagement.Models;
using InventoryManagement.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace InventoryManagement
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            // Add services to the container.
            var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseNpgsql(connectionString));
            builder.Services.AddDatabaseDeveloperPageExceptionFilter();

            builder.Services.AddDefaultIdentity<AppUser>()
                .AddEntityFrameworkStores<ApplicationDbContext>();
            builder.Services.AddRazorPages();
            builder.Services.AddScoped<UserSearch>();
            builder.Services.AddScoped<TagSearch>();
            builder.Services.AddScoped<ItemLikeService>();

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

            app.UseAuthentication();
            app.UseAuthorization();


            app.MapGet("/api/search/users", async (string query, UserSearch search) =>
            {
                var results = await search.Search(query);
                return Results.Ok(results);
            });

            app.MapGet("/api/search/tags", async (string query, TagSearch search) =>
            {
                var results = await search.Search(query, 5);
                return Results.Ok(results);
            });

            app.MapPost("/api/items/{guid:guid}/like", async (Guid guid, ItemLikeService likes, HttpContext http) =>
            {
                var userId = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();
                var (liked, count) = await likes.ToggleLikeByItemGuidAsync(guid, userId);
                return Results.Ok(new { liked, count });
            }).RequireAuthorization();

            app.MapStaticAssets();
            app.MapRazorPages()
               .WithStaticAssets();

            app.Run();
        }
    }
}
