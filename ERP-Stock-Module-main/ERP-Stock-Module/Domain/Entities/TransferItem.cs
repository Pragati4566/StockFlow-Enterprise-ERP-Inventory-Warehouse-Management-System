using Domain.Enums;

namespace Domain.Entities
{
    public class TransferItem : AuditableEntity
    {
  
        public int TransferOrderId { get; set; }
        public int ProductId { get; set; }
        public decimal QuantitySent { get; set; }
        public decimal? QuantityReceived { get; set; }
        public TransferStatus Status { get; set; } = TransferStatus.Pending;
        public DateTime? ReceivedDate { get; set; }

        // Navigation
        public TransferOrder TransferOrder { get; set; } = null!;
        public Product Product { get; set; } = null!;
    }

}