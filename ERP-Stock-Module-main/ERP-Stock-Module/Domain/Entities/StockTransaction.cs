namespace Domain.Entities
{
    public class StockTransaction : AuditableEntity
    {
     
        public int LotId { get; set; }
        public decimal Quantity { get; set; } // موجب = إضافة، سالب = خصم
        public DateTime TransactionDate { get; set; }
        public string? Reference { get; set; } // مثل: Sale-15, Transfer-88-Item-3

        // Navigation
        public Lot Lot { get; set; } = null!;
    }
}