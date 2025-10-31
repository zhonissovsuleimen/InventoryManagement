namespace InventoryManagement.Models.Inventory.CustomId.Element
{
    public class FixedTextElement : AbstractElement
    {
        public string FixedText { get; set; } = string.Empty;
        public override string Generate(Random rng)
        {
            return FixedText;
        }
    }
}
