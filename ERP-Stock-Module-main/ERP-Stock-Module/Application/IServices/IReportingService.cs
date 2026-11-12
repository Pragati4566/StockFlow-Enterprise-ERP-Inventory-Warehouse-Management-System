using Application.DTOs;

namespace Application.IServices
{
    public interface IReportingService
    {
        Task<PagedResult<StockMovementDto>> GetMovementsAsync(ReportFilter filter);
        Task<List<StockSummaryDto>> GetSummaryAsync(ReportFilter filter);
    }
}