namespace Application.DTOs
{
    public class TransferResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public int? TransferOrderId { get; set; }
        public int? TransferItemId { get; set; } 
        public decimal? QuantityReceived { get; set; } 
    }
}