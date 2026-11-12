namespace Application.DTOs
{
    public class ReportFilter
    {
        public int? WarehouseId { get; set; }
        public int? SupplierId { get; set; }
        public string? ProductCategory { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
}