namespace InventoryManagement.Models.Inventory
{
    using System.ComponentModel.DataAnnotations;

    public class CustomField : IValidatableObject
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public short? Position { get; set; }
        public bool IsUsed { get; set; } = false;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!IsUsed)
                yield break;

            if (string.IsNullOrWhiteSpace(Title))
            {
                yield return new ValidationResult(
                    "The title of the custom fields is not valid.",
                    [nameof(Title)]
                );
            }

            if (string.IsNullOrWhiteSpace(Description))
            {
                yield return new ValidationResult(
                    "The description of the custom fields is not valid.",
                    [nameof(Description)]
                );
            }
        }
    }
}
