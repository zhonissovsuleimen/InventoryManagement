using InventoryManagement.Data;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Services;

public record InventorySearchResult(Guid Guid, string Title, string Description, string? ImageUrl, string? OwnerId, IReadOnlyList<string> MatchedTags);

public class InventorySearch
{
    private readonly ApplicationDbContext _db;
    public InventorySearch(ApplicationDbContext db) => _db = db;

    public async Task<List<InventorySearchResult>> SearchAsync(string query, int limit = 3)
    {
        var term = (query ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(term)) return new List<InventorySearchResult>();

        const double trigramThreshold = 0.05;

        var results = await _db.Inventories
            .AsNoTracking()
            .Include(i => i.Owner)
            .Where(i => i.IsPublic)
            .Where(i =>
                i.SearchVector!.Matches(EF.Functions.PlainToTsQuery("simple", term)) ||
                EF.Functions.TrigramsSimilarity(i.Title, term) > trigramThreshold ||
                EF.Functions.TrigramsSimilarity(i.Description ?? "", term) > trigramThreshold ||
                i.Tags.Any(t => EF.Functions.TrigramsSimilarity(t.Name, term) > trigramThreshold || EF.Functions.ILike(t.Name, term + "%"))
            )
            .Select(i => new
            {
                i,
                Rank =
                    i.SearchVector!.Rank(EF.Functions.PlainToTsQuery("simple", term)) +
                    EF.Functions.TrigramsSimilarity(i.Title, term) +
                    EF.Functions.TrigramsSimilarity(i.Description ?? "", term)
            })
            .OrderByDescending(x => x.Rank)
            .Take(limit)
            .Select(x => new InventorySearchResult(
                x.i.Guid,
                x.i.Title,
                x.i.Description,
                x.i.ImageUrl,
                x.i.Owner != null ? x.i.Owner.Id : null,
                x.i.Tags
                    .Where(t => EF.Functions.TrigramsSimilarity(t.Name, term) > trigramThreshold || EF.Functions.ILike(t.Name, term + "%"))
                    .OrderBy(t => t.Name)
                    .Select(t => t.Name)
                    .Take(5)
                    .ToList()
            ))
            .ToListAsync();

        return results;
    }
}
