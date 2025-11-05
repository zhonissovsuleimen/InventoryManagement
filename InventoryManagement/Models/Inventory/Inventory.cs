using NpgsqlTypes;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace InventoryManagement.Models.Inventory
{
    //todo discussions, tags
    public class Inventory
    {
        public int Id { get; set; }
        public Guid Guid { get; set; } = Guid.NewGuid();
        
        [ConcurrencyCheck]
        public int Version { get; set; } = 0;

        [Required]
        public string Title { get; set; }
        [Required]
        public string Description { get; set; }
        [Required]
        public Category Category { get; set; }
        public string? ImageUrl { get; set; }
        [Required]
        [DisplayName("Access Level")]
        public bool IsPublic { get; set; } = false;
        [Required]
        public List<AppUser> AllowedUsers { get; set; } = [];

        public List<Item> Items { get; set; } = [];

        public AppUser Owner { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public NpgsqlTsVector? SearchVector { get; set; }

        public CustomId.CustomId CustomId { get; set; }

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
        public CustomField? LinkLine1 { get; set; }
        public CustomField? LinkLine2 { get; set; }
        public CustomField? LinkLine3 { get; set; }

        public List<Tag> Tags { get; set; } = [];

        public string GenerateCustomId(int? seed = null)
        {
            if (CustomId == null || CustomId.Elements == null || CustomId.Elements.Count == 0)
            {
                return Guid.NewGuid().ToString();
            }

            var rng = seed == null ? new Random() : new Random((int)seed);
            var sb = new StringBuilder();

            var elems = CustomId.Elements
                .Where(e => e != null)
                .OrderBy(e => e.Position ?? short.MaxValue)
                .ThenBy(e => e.Id)
                .ToList();

            foreach (var element in elems)
            {
                if (element == null) continue;
                if (element.SeparatorBefore != null) sb.Append(element.SeparatorBefore);
                sb.Append(element.Generate(rng));
                if (element.SeparatorAfter != null) sb.Append(element.SeparatorAfter);
            }

            return sb.ToString();
        }

    }
}
