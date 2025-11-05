using InventoryManagement.Data;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Services
{
    public record UserSearchResult(string Guid, string FirstName, string? LastName, string? Email);

    public class UserSearch
    {
        private readonly ApplicationDbContext _db;
        public UserSearch(ApplicationDbContext db) => _db = db;

        public async Task<List<UserSearchResult>> Search(string query, int limit = 3)
        {
            if (string.IsNullOrWhiteSpace(query)) return [];

            const double threshold = 0.05;

            var users = await _db.Users.Where(u =>
                u.SearchVector!.Matches(EF.Functions.PlainToTsQuery("simple", query)) ||
                EF.Functions.TrigramsSimilarity(u.FirstName, query) > threshold ||
                EF.Functions.TrigramsSimilarity(u.LastName ?? "", query) > threshold ||
                EF.Functions.TrigramsSimilarity(u.Email ?? "", query) > threshold)
            .Select(u => new
            {
                u,
                Rank =
                    u.SearchVector!.Rank(EF.Functions.PlainToTsQuery("simple", query)) +
                    EF.Functions.TrigramsSimilarity(u.FirstName, query) +
                    EF.Functions.TrigramsSimilarity(u.LastName ?? "", query) +
                    EF.Functions.TrigramsSimilarity(u.Email ?? "", query)
            })
            .OrderByDescending(x => x.Rank)
            .Take(limit)
            .Select(x => x.u)
            .ToListAsync();


            var results = users.Select(u => 
                new UserSearchResult(
                    Guid: u.Id,
                    FirstName: u.FirstName,
                    LastName: u.LastName,
                    Email: u.Email
                )
            ).ToList();

            return results;
        }
    }
}
