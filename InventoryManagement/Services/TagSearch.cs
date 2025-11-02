using InventoryManagement.Data;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Services
{
    public class TagSearch
    {
        private readonly ApplicationDbContext _db;

        public TagSearch(ApplicationDbContext db) => _db = db;

        public async Task<IReadOnlyList<Result>> Search(string query, int limit = 5)
        {
            var term = (query ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(term))
                return [];

            var results = await _db.Tags
                .AsNoTracking()
                .Where(t => EF.Functions.ILike(t.Name, term + "%"))
                .OrderBy(t => t.Name)
                .Take(limit)
                .Select(t => new Result(t.Id, t.Name))
                .ToListAsync();

            return results;
        }

        public record Result(int Id, string Name);
    }
}
