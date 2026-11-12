namespace Domain.Entities
{
    public class Lot : AuditableEntity
    {
      
        public int ProductId { get; set; }
        public int? SupplierId { get; set; }
        public int WarehouseId { get; set; }
      
        public decimal QuantityReceived { get; set; }
        public decimal RemainingQuantity { get; set; }
        public decimal UnitCost { get; set; }
        public DateTime ReceivedDate { get; set; }

        // Navigation Properties (اختيارية، لكنها مفيدة)
        public Product Product { get; set; } = null!;
        public Supplier? Supplier { get; set; } = null!;
        public Warehouse Warehouse { get; set; } = null!;
        public ICollection<StockTransaction> StockTransactions { get; set; } = new List<StockTransaction>();
    }
}