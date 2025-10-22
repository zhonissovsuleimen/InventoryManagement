namespace InventoryManagement.Models.CustomId.Element
{
    public class DateTimeElement : AbstractElement
    {
        public string DateTimeFormat { get; set; } = "yyyy";

        public override string Generate(int seed)
        {
            return DateTime.UtcNow.ToString(DateTimeFormat);
        }
    }
}
