namespace Application.DTOs
{
    public class LotDeduction
    {
        public int LotId { get; set; }
        public decimal QuantityDeducted { get; set; }
        public decimal UnitCost { get; set; }
    }
}