using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Models
{
    public class ItemLike
    {
        [Required]
        public Item Item { get; set; }

        [Required]
        public AppUser User { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
