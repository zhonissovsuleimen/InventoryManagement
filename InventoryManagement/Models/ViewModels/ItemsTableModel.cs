namespace InventoryManagement.Models.ViewModels
{
    public class ItemsTableModel
    {
        public IReadOnlyList<Item> Items { get; set; } = System.Array.Empty<Item>();
        public bool ShowInventoryColumn { get; set; }
    }
}
