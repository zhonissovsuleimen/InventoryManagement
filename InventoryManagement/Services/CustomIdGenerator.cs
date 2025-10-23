using InventoryManagement.Data;
using InventoryManagement.Models.CustomId;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace InventoryManagement.Services
{
    public class CustomIdGenerator
    {
        // Request DTOs moved here to keep API surface cohesive
        public class PreviewRequest
        {
            public int Seed { get; set; }
            public List<ElementInput> Elements { get; set; } = [];
        }

        public class ElementInput
        {
            public string Type { get; set; } = string.Empty;
            public string? SeparatorBefore { get; set; }
            public string? SeparatorAfter { get; set; }

            public string? FixedText { get; set; }
            public string? DateTimeFormat { get; set; }
            public string? PaddingChar { get; set; }
            public string? Radix { get; set; }
        }

        public string Generate(int seed, CustomId query)
        {
            var rng = new Random(seed);
            StringBuilder sb = new StringBuilder();

            foreach (var element in query.Elements)
            {
                sb.Append(element.SeparatorBefore);
                sb.Append(element.Generate(rng));
                sb.Append(element.SeparatorAfter);
            }

            return sb.ToString();
        }
    }
}
