using NpgsqlTypes;
using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Models.Inventory
{
    //todo custom id, items, discussions, tags
    public class Inventory
    {
        public int Id { get; set; }
        public Guid Guid { get; set; } = Guid.NewGuid();
        [Required]
        public string Title { get; set; }
        [Required]
        public string Description { get; set; }
        [Required]
        public Category Category { get; set; }
        public string? ImageUrl { get; set; }
        [Required]
        public bool IsPublic { get; set; } = false;
        [Required]
        public List<AppUser> AllowedUsers { get; set; } = [];

        public AppUser Owner { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public NpgsqlTsVector? SearchVector { get; set; }

        public CustomField? SingleLine1 { get; set; }
        public CustomField? SingleLine2 { get; set; }
        public CustomField? SingleLine3 { get; set; }
        public CustomField? MultiLine1 { get; set; }
        public CustomField? MultiLine2 { get; set; }
        public CustomField? MultiLine3 { get; set; }
        public CustomField? NumericLine1 { get; set; }
        public CustomField? NumericLine2 { get; set; }
        public CustomField? NumericLine3 { get; set; }
        public CustomField? BoolLine1 { get; set; }
        public CustomField? BoolLine2 { get; set; }
        public CustomField? BoolLine3 { get; set; }

    }
}
