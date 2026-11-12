namespace Domain.Entities
{
    public class StockThreshold : AuditableEntity
    {
   
        public int ProductId { get; set; }
        public int WarehouseId { get; set; }
        public decimal MinQuantity { get; set; }

        // Navigation
        public Product Product { get; set; } = null!;
        public Warehouse Warehouse { get; set; } = null!;
    }
}