using InventoryManagement.Data;
using InventoryManagement.Database;
using InventoryManagement.Models;
using InventoryManagement.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Localization;
using System.Globalization;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using AspNet.Security.OAuth.GitHub;

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

            builder.Services
                .AddDefaultIdentity<AppUser>(options =>
                {
                    options.SignIn.RequireConfirmedAccount = false;
                })
                .AddEntityFrameworkStores<ApplicationDbContext>();

            builder.Services.AddScoped<GoogleSignInService>();
            builder.Services.AddScoped<GitHubSignInService>();
            builder.Services.AddSingleton<SalesforceService>();

            builder.Services
                .AddAuthentication()
                .AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
                {
                    options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? string.Empty;
                    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? string.Empty;
                    options.CallbackPath = "/signin-google";
                    options.SaveTokens = true;
                    options.ClaimActions.MapJsonKey(ClaimTypes.GivenName, "given_name");
                    options.ClaimActions.MapJsonKey(ClaimTypes.Surname, "family_name");
                    options.Events = new Microsoft.AspNetCore.Authentication.OAuth.OAuthEvents
                    {
                        OnCreatingTicket = async context =>
                        {
                            var handler = context.HttpContext.RequestServices.GetRequiredService<GoogleSignInService>();
                            await handler.HandleCreatingTicket(context);
                        }
                    };
                })
                .AddGitHub(GitHubAuthenticationDefaults.AuthenticationScheme, options =>
                {
                    options.ClientId = builder.Configuration["Authentication:GitHub:ClientId"] ?? string.Empty;
                    options.ClientSecret = builder.Configuration["Authentication:GitHub:ClientSecret"] ?? string.Empty;
                    options.CallbackPath = "/signin-github";
                    options.SaveTokens = true;

                    // Request user email scope to access primary email via API when not in ID token
                    options.Scope.Add("user:email");

                    // Map name/email if present
                    options.ClaimActions.MapJsonKey(ClaimTypes.Name, "name");
                    options.ClaimActions.MapJsonKey(ClaimTypes.Email, "email");

                    options.Events = new Microsoft.AspNetCore.Authentication.OAuth.OAuthEvents
                    {
                        OnCreatingTicket = async context =>
                        {
                            var handler = context.HttpContext.RequestServices.GetRequiredService<GitHubSignInService>();
                            await handler.HandleCreatingTicket(context);
                        }
                    };
                });

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
            builder.Services.AddScoped<InventorySearch>();

            builder.Services.AddHttpClient();
            builder.Services.AddScoped<DropboxService>();

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var db = services.GetRequiredService<ApplicationDbContext>();

                    if (app.Environment.IsDevelopment())
                    {
                        Console.WriteLine("Applying migrations...");
                        await db.Database.MigrateAsync();
                        var userManager = services.GetRequiredService<UserManager<AppUser>>();
                        if (!await userManager.Users.AnyAsync())
                        {
                            Console.WriteLine("Reseeding data...");
                            await Seed.SeedAll(services);
                            Console.WriteLine("Development database reseed completed.");
                        }
                        else
                        {
                            Console.WriteLine("Users already exist, skipping reseed.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Database migration/seed failed: {ex}");
                    throw;
                }
            }

            if (app.Environment.IsDevelopment())
            {
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            var locOptions = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
            app.UseRequestLocalization(locOptions);

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            // Block/redirect Register everywhere
            app.MapGet("/Identity/Account/Register", () => Results.Redirect("/Identity/Account/Login"));
            app.MapPost("/Identity/Account/Register", () => Results.Redirect("/Identity/Account/Login"));
            app.MapGet("/Account/Register", () => Results.Redirect("/Identity/Account/Login"));
            app.MapPost("/Account/Register", () => Results.Redirect("/Identity/Account/Login"));

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
                var results = await search.Search(query, 3);
                return Results.Ok(results);
            });

            app.MapGet("/api/search/inventories", async (string query, InventorySearch search) =>
            {
                var results = await search.SearchAsync(query, 3);
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
