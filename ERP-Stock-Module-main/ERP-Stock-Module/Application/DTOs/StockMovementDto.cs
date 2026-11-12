namespace Application.DTOs
{
    public class StockMovementDto
    {
        public int TransactionId { get; set; }
        public DateTime Date { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
    }
}