namespace Application.DTOs
{
    public class AllocationRequest
    {
        public int ProductId { get; set; }
        public int WarehouseId { get; set; }
        public decimal Quantity { get; set; }
    }
}