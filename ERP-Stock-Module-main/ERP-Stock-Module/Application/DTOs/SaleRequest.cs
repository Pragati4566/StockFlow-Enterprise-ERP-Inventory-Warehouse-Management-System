namespace Application.DTOs
{
    public class SaleRequest
    {
        public int WarehouseId { get; set; }
        public List<SaleItem> Items { get; set; } = new();
    }

    public class SaleItem
    {
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
    }
}