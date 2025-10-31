using System;
using System.Text;

namespace InventoryManagement.Models.Inventory.CustomId.Element
{
    public abstract class AbstractElement
    {
        public int Id { get; set; }
        public char? SeparatorBefore { get; set; } = null;
        public char? SeparatorAfter { get; set; } = null;
        public short? Position { get; set; }

        public CustomId CustomId { get; set; }

        public abstract string Generate(Random rng);
    }

}
