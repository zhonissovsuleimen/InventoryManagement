using System.ComponentModel.DataAnnotations;
using InventoryManagement.Models;
using InventoryManagement.Models.Inventory;

namespace InventoryManagement.Models.Discussion
{
    public class DiscussionPost
    {
        public int Id { get; set; }
        public Guid Guid { get; set; } = Guid.NewGuid();

        public int InventoryId { get; set; }
        public Inventory.Inventory Inventory { get; set; } = default!;

        [Required]
        public string UserId { get; set; } = default!;
        public AppUser User { get; set; } = default!;

        [Required]
        [MaxLength(4000)]
        public string ContentMarkdown { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
