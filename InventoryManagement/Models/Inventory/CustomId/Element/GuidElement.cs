namespace InventoryManagement.Models.Inventory.CustomId.Element
{
    public class GuidElement : AbstractElement
    {
        public override string Generate(Random rng)
        {
            var guidBytes = new byte[16];
            rng.NextBytes(guidBytes);
            var guid = new Guid(guidBytes);
            return guid.ToString();
        }
    }
}
