using InventoryManagement.Data;
using InventoryManagement.Database;
using InventoryManagement.Models;
using InventoryManagement.Models.CustomId;
using InventoryManagement.Models.CustomId.Element;
using InventoryManagement.Services;
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
            builder.Services.AddScoped<UserSearch>();
            builder.Services.AddScoped<CustomIdGenerator>();

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
            app.MapPost("/api/generate/customId", (CustomIdGenerator.PreviewRequest request, CustomIdGenerator gen) =>
            {
                static char? ParseChar(string? s) => string.IsNullOrEmpty(s) ? null : s[0];
                static Radix ParseRadix(string? s) => s switch
                {
                    "2" => Models.CustomId.Element.Radix.Binary,
                    "8" => Models.CustomId.Element.Radix.Octal,
                    "10" => Models.CustomId.Element.Radix.Decimal,
                    "16" => Models.CustomId.Element.Radix.Hexadecimal,
                    _ => Models.CustomId.Element.Radix.Decimal
                };

                var customId = new CustomId
                {
                    Guid = Guid.NewGuid(),
                    Elements = []
                };

                foreach (var e in request.Elements)
                {
                    AbstractElement? element = e.Type switch
                    {
                        "FixedText" => new Models.CustomId.Element.FixedTextElement { FixedText = e.FixedText ?? string.Empty },
                        "Digit6" => new Models.CustomId.Element.Digit6Element { PaddingChar = ParseChar(e.PaddingChar), Radix = ParseRadix(e.Radix) },
                        "Digit9" => new Models.CustomId.Element.Digit9Element { PaddingChar = ParseChar(e.PaddingChar), Radix = ParseRadix(e.Radix) },
                        "Bit20" => new Models.CustomId.Element.Bit20Element { PaddingChar = ParseChar(e.PaddingChar), Radix = ParseRadix(e.Radix) },
                        "Bit32" => new Models.CustomId.Element.Bit32Element { PaddingChar = ParseChar(e.PaddingChar), Radix = ParseRadix(e.Radix) },
                        "DateTime" => new Models.CustomId.Element.DateTimeElement { DateTimeFormat = string.IsNullOrWhiteSpace(e.DateTimeFormat) ? "yyyy" : e.DateTimeFormat },
                        "Guid" => new Models.CustomId.Element.GuidElement(),
                        _ => null
                    };
                    if (element == null) continue;
                    element.SeparatorBefore = ParseChar(e.SeparatorBefore);
                    element.SeparatorAfter = ParseChar(e.SeparatorAfter);
                    customId.Elements.Add(element);
                }

                var result = gen.Generate(request.Seed, customId);
                return Results.Text(result, "text/plain");
            });


            app.MapStaticAssets();
            app.MapRazorPages()
               .WithStaticAssets();

            app.Run();
        }
    }
}
