namespace InventoryManagement.Models.CustomId.Element
{
    public class Bit20Element : AbstractNumericElement
    {
        public override string Generate(Random rng)
        {
            uint value = (uint)rng.NextInt64(0xFFFFF + 1);
            var stringValue = Convert.ToString(value, (int)Radix);
            if (PaddingChar.HasValue)
            {
                stringValue = Radix switch
                {
                    Radix.Binary => stringValue.PadLeft(20, PaddingChar.Value),
                    Radix.Octal => stringValue.PadLeft(7, PaddingChar.Value),
                    Radix.Decimal => stringValue.PadLeft(7, PaddingChar.Value),
                    Radix.Hexadecimal => stringValue.PadLeft(5, PaddingChar.Value),
                    _ => stringValue,
                };
            }

            return stringValue;
        }
    }
}
