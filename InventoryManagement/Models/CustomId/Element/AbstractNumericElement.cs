namespace InventoryManagement.Models.CustomId.Element
{
    public abstract class AbstractNumericElement : AbstractElement
    {
        public Radix Radix { get; set; } = Radix.Decimal;
        public char? PaddingChar { get; set; } = null;
    }
}
