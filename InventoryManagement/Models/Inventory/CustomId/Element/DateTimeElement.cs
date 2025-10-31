namespace InventoryManagement.Models.Inventory.CustomId.Element
{
    public class DateTimeElement : AbstractElement
    {
        public string DateTimeFormat { get; set; } = "yyyy";

        public override string Generate(Random rng)
        {
            return DateTime.UtcNow.ToString(DateTimeFormat);
        }
    }
}
