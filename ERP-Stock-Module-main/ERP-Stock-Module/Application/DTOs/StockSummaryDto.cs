namespace Application.DTOs
{
    public class StockSummaryDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
     
        public decimal TotalIn { get; set; }
        public decimal TotalOut { get; set; }
        public decimal NetChange => TotalIn + TotalOut;
    }
}