using Application.DTOs;
using Application.IServices;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;

namespace Infrastructure.Services
{
    public class ReportingService : IReportingService
    {
        private readonly AppDbContext _context;

        public ReportingService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PagedResult<StockMovementDto>> GetMovementsAsync(ReportFilter filter)
        {
            var query = _context.StockTransactions
                .AsNoTracking()
                .Include(t => t.Lot)
                    .ThenInclude(l => l.Product)
                .Include(t => t.Lot)
                    .ThenInclude(l => l.Warehouse)
                .Include(t => t.Lot)
                    .ThenInclude(l => l.Supplier)
                .AsQueryable();

            // تطبيق الفلاتر
            if (filter.WarehouseId.HasValue)
                query = query.Where(t => t.Lot.WarehouseId == filter.WarehouseId.Value);
            if (filter.SupplierId.HasValue)
                query = query.Where(t => t.Lot.SupplierId == filter.SupplierId.Value);
            if (!string.IsNullOrWhiteSpace(filter.ProductCategory))
                query = query.Where(t => t.Lot.Product.Category == filter.ProductCategory);

            if (filter.FromDate.HasValue)
                query = query.Where(t => t.TransactionDate >= filter.FromDate.Value);
            if (filter.ToDate.HasValue)
                query = query.Where(t => t.TransactionDate <= filter.ToDate.Value);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(t => t.TransactionDate)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(t => new StockMovementDto
                {
                    TransactionId = t.Id,
                    Date = t.TransactionDate,
                    ProductName = t.Lot.Product.Name,
                    WarehouseName = t.Lot.Warehouse.Name,
                    SupplierName = t.Lot.Supplier != null ? t.Lot.Supplier.Name : "نقل",
                    Quantity = t.Quantity
                })
                .ToListAsync();

            return new PagedResult<StockMovementDto>
            {
                Items = items,
                TotalCount = totalCount,
                Page = filter.Page,
                PageSize = filter.PageSize
            };
        }

        public async Task<List<StockSummaryDto>> GetSummaryAsync(ReportFilter filter)
        {
            var query = _context.StockTransactions
                .AsNoTracking()
                .Include(t => t.Lot)
                    .ThenInclude(l => l.Product)
                .AsQueryable();

            if (filter.WarehouseId.HasValue)
                query = query.Where(t => t.Lot.WarehouseId == filter.WarehouseId.Value);
            if (filter.SupplierId.HasValue)
                query = query.Where(t => t.Lot.SupplierId == filter.SupplierId.Value);
            if (!string.IsNullOrWhiteSpace(filter.ProductCategory))
                query = query.Where(t => t.Lot.Product.Category == filter.ProductCategory);

            if (filter.FromDate.HasValue)
                query = query.Where(t => t.TransactionDate >= filter.FromDate.Value);
            if (filter.ToDate.HasValue)
                query = query.Where(t => t.TransactionDate <= filter.ToDate.Value);

            var summary = await query
                .GroupBy(t => new { t.Lot.ProductId, t.Lot.Product.Name })
                .Select(g => new StockSummaryDto
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.Name,
                   
                    TotalIn = g.Sum(t => t.Quantity > 0 ? t.Quantity : 0),
                    TotalOut = g.Sum(t => t.Quantity < 0 ? t.Quantity : 0)
                })
                .ToListAsync();

            return summary;
        }
    }
}