using Application.DTOs;

namespace Application.IServices
{
    public interface ISellService
    {
        Task<SaleResult> SellAsync(SaleRequest request);
    }
}