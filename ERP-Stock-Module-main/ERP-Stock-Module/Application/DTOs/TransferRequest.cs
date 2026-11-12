namespace Application.DTOs
{
    public class TransferRequest
    {
        public int SourceWarehouseId { get; set; }
        public int DestinationWarehouseId { get; set; }
        public List<TransferItemDto> Items { get; set; } = new();
    }

    public class TransferItemDto
    {
        public int ProductId { get; set; }
        public decimal Quantity { get; set; }
    }
}