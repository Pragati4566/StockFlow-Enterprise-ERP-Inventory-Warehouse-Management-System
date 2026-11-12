using Domain.Enums;

namespace Domain.Entities
{
    public class TransferOrder : AuditableEntity
    {
       
        public int SourceWarehouseId { get; set; }
        public int DestinationWarehouseId { get; set; }
        public DateTime CreatedDate { get; set; }
        public TransferStatus Status { get; set; } = TransferStatus.Pending;

        // Navigation
        public Warehouse SourceWarehouse { get; set; } = null!;
        public Warehouse DestinationWarehouse { get; set; } = null!;
        public ICollection<TransferItem> TransferItems { get; set; } = new List<TransferItem>();
    }

}