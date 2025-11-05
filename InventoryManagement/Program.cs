using InventoryManagement.Data;
using InventoryManagement.Database;
using InventoryManagement.Models;
using InventoryManagement.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using Microsoft.Extensions.Options;

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

            // Localization
            builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
            builder.Services.AddRazorPages()
                .AddViewLocalization()
                .AddDataAnnotationsLocalization();

            var supportedCultures = new[] { new CultureInfo("en"), new CultureInfo("ru") };
            builder.Services.Configure<RequestLocalizationOptions>(options =>
            {
                options.DefaultRequestCulture = new RequestCulture("en");
                options.SupportedCultures = supportedCultures.ToList();
                options.SupportedUICultures = supportedCultures.ToList();
                options.RequestCultureProviders = new List<IRequestCultureProvider>
                {
                    new QueryStringRequestCultureProvider(),
                    new CookieRequestCultureProvider()
                };
            });

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

            // Request localization should be early in the pipeline
            var locOptions = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
            app.UseRequestLocalization(locOptions);

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            // Endpoint to switch UI culture via cookie
            app.MapGet("/set-language", (string culture, string? returnUrl, HttpContext http) =>
            {
                var ci = new CultureInfo(culture);
                http.Response.Cookies.Append(
                    CookieRequestCultureProvider.DefaultCookieName,
                    CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(ci)),
                    new CookieOptions
                    {
                        Expires = DateTimeOffset.UtcNow.AddYears(1),
                        IsEssential = true,
                        SameSite = SameSiteMode.Lax,
                        Secure = http.Request.IsHttps
                    }
                );
                var target = string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl;
                return Results.LocalRedirect(target);
            });

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
