namespace InventoryManagement.Models.Inventory.CustomId.Element
{
    public class SequentialElement : AbstractNumericElement
    {
        public override string Generate(Random rng)
        {
            // Preview placeholder: start from 1
            return "1";
        }
    }
}
