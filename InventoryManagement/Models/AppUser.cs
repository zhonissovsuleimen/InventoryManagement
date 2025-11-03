using Microsoft.AspNetCore.Identity;
using NpgsqlTypes;

namespace InventoryManagement.Models
{
    public class AppUser : IdentityUser
    {
        public int SequentialId { get; set; }
        public string FirstName { get; set; }
        public string? LastName { get; set; }

        public bool IsAdmin { get; set; }

        public NpgsqlTsVector? SearchVector { get; set; }

        public List<Inventory.Inventory> OwnedInventories { get; set; } = [];
        public List<Inventory.Inventory> AllowedInventories { get; set; } = [];
    }
}
