namespace InventoryManagement.Models.CustomId.Element
{
    public class GuidElement : AbstractElement
    {
        public override string Generate(int seed)
        {
            var rng = new Random(seed);
            var guidBytes = new byte[16];
            rng.NextBytes(guidBytes);
            var guid = new Guid(guidBytes);
            return guid.ToString();
        }
    }
}
