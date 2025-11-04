namespace InventoryManagement.Models.ViewModels
{
    public class ItemsTableModel
    {
        public IReadOnlyList<Item> Items { get; set; } = System.Array.Empty<Item>();
        public bool ShowInventoryColumn { get; set; }
        // When true, the table renders selection checkboxes and a toolbar above it
        public bool EnableSelection { get; set; }
        // Endpoint that deletes selected items. Expected to accept POST JSON { guids: Guid[] } and return { deleted: Guid[] }
        public string? DeleteUrl { get; set; }
    }
}
