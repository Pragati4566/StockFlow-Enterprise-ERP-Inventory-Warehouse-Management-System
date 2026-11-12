namespace Application.DTOs
{
    public class ReceiveStockRequest
    {
        public int ProductId { get; set; }
        public int? SupplierId { get; set; }
        public int WarehouseId { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitCost { get; set; }
    }
}