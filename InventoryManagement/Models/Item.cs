using InventoryManagement.Models.Inventory;

namespace InventoryManagement.Models
{
    public class Item
    {
        public int Id { get; set; }
        public Guid Guid { get; set; } = Guid.NewGuid();
        public  Inventory.Inventory Inventory { get; set; }
        public AppUser Owner { get; set; }
        public string CustomId { get; set; }

        public string? SingleLine1 { get; set; }
        public string? SingleLine2 { get; set; }
        public string? SingleLine3 { get; set; }
        public string? MultiLine1 { get; set; }
        public string? MultiLine2 { get; set; }
        public string? MultiLine3 { get; set; }
        public double? NumericLine1 { get; set; }
        public double? NumericLine2 { get; set; }
        public double? NumericLine3 { get; set; }
        public bool? BoolLine1 { get; set; }
        public bool? BoolLine2 { get; set; }
        public bool? BoolLine3 { get; set; }
    }
}
