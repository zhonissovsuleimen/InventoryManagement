namespace InventoryManagement.Models.Inventory.CustomId.Element
{
    public class Bit32Element : AbstractNumericElement
    {
        public override string Generate(Random rng)
        {
            uint value = (uint)rng.NextInt64(0xFFFFFFFFL + 1);
            var stringValue = Convert.ToString(value, (int)Radix);
            if (PaddingChar.HasValue)
            {
                stringValue = Radix switch
                {
                    Radix.Binary => stringValue.PadLeft(32, PaddingChar.Value),
                    Radix.Octal => stringValue.PadLeft(11, PaddingChar.Value),
                    Radix.Decimal => stringValue.PadLeft(10, PaddingChar.Value),
                    Radix.Hexadecimal => stringValue.PadLeft(8, PaddingChar.Value),
                    _ => stringValue,
                };
            }

            return stringValue;
        }
    }
}
