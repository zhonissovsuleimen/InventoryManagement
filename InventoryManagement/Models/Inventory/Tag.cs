using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Models.Inventory
{
    public class Tag
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(64)]
        public string Name { get; set; } = string.Empty;

        public List<Inventory> Inventories { get; set; } = [];
    }
}
