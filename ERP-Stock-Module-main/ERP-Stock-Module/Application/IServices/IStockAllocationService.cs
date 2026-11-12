using Application.DTOs;

namespace Application.IServices
{
    public interface IStockAllocationService
    {
        Task<AllocationResult> AllocateAsync(AllocationRequest request);
    }
}