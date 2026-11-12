namespace Application.DTOs
{
    public class AllocationResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public List<LotDeduction> Deductions { get; set; } = new();
    }
}