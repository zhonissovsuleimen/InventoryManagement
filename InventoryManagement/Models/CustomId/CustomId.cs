using InventoryManagement.Models.CustomId.Element;

namespace InventoryManagement.Models.CustomId
{
    public class CustomId
    {
        public int Id { get; set; }
        public Guid Guid { get; set; } = Guid.NewGuid();
        public List<AbstractElement> Elements { get; set; } = [];
    }
}
