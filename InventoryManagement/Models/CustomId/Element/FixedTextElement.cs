namespace InventoryManagement.Models.CustomId.Element
{
    public class FixedTextElement : AbstractElement
    {
        public string FixedText { get; set; } = string.Empty;
        public override string Generate(int seed)
        {
            return FixedText;
        }
    }
}
