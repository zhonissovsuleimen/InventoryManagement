namespace InventoryManagement.Models.Inventory.CustomId.Element
{
    public class Digit6Element : AbstractNumericElement
    {
        public override string Generate(Random rng)
        {
            int digits = 6;

            long maxValueExclusive = (long)Math.Pow((double)Radix, digits);
            long value = rng.NextInt64(maxValueExclusive);
            var stringValue = Convert.ToString(value, (int)Radix);

            if (PaddingChar.HasValue)
            {
                stringValue = stringValue.PadLeft(digits, PaddingChar.Value);
            }

            return stringValue;
        }
    }
}
