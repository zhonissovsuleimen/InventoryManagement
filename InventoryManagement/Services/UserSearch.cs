using InventoryManagement.Data;
using InventoryManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryManagement.Services
{
    public record UserSearchResult(string Guid, string FirstName, string? LastName);

    public class UserSearch
    {
        private readonly ApplicationDbContext _db;
        public UserSearch(ApplicationDbContext db) => _db = db;

        public async Task<List<UserSearchResult>> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return [];

            const double threshold = 0.15;

            var users = await _db.Users.Where(u =>
                u.SearchVector!.Matches(EF.Functions.PlainToTsQuery("simple", query)) ||
                EF.Functions.TrigramsSimilarity(u.FirstName, query) > threshold ||
                EF.Functions.TrigramsSimilarity(u.LastName ?? "", query) > threshold)
            .Select(u => new
            {
                u,
                Rank =
                    u.SearchVector!.Rank(EF.Functions.PlainToTsQuery("simple", query)) +
                    EF.Functions.TrigramsSimilarity(u.FirstName, query) +
                    EF.Functions.TrigramsSimilarity(u.LastName ?? "", query)
            })
            .OrderByDescending(x => x.Rank)
            .Take(5)
            .Select(x => x.u)
            .ToListAsync();


            var results = users.Select(u => 
                new UserSearchResult(
                    Guid: u.Id,
                    FirstName: u.FirstName,
                    LastName: u.LastName
                )
            ).ToList();

            return results;
        }
    }
}
