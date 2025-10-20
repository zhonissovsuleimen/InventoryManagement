using System.ComponentModel.DataAnnotations;

namespace InventoryManagement.Models.Inventory
{
    public class Category
    {
        public int Id { get; set; }
        public Guid Guid { get; set; } = Guid.NewGuid();
        [Required]
        public string Name { get; set; }

        public List<Inventory> Inventories { get; set; } = [];
    }
}
