using InventoryManagement.Models.Inventory.CustomId.Element;

namespace InventoryManagement.Models.Inventory.CustomId
{
    public class CustomId
    {
        public int Id { get; set; }
        public Guid Guid { get; set; } = Guid.NewGuid();
        public List<AbstractElement> Elements { get; set; } = [];
    }
}
