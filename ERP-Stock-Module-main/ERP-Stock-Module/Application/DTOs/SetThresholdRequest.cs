namespace Application.DTOs
{
    public class SetThresholdRequest
    {
        public int ProductId { get; set; }
        public int WarehouseId { get; set; }
        public decimal MinQuantity { get; set; }
    }
}