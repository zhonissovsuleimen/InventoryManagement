namespace InventoryManagement.Models.CustomId.Element
{
    public class Digit6Element : AbstractNumericElement
    {
        public override string Generate(int seed)
        {
            int digits = 6;
            var rng = new Random(seed);

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
