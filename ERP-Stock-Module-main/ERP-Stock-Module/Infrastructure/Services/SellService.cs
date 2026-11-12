using Application.DTOs;
using Application.IServices;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;
using Microsoft.Extensions.Logging;



namespace Infrastructure.Services
{
    public class SellService : ISellService
    {
        private readonly AppDbContext _context;
        private readonly IStockAllocationService _allocationService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<SellService> _logger;
        public SellService(AppDbContext context, IStockAllocationService allocationService, INotificationService notificationService, ILogger<SellService> logger)
        {
            _context = context;
            _allocationService = allocationService;
            _notificationService = notificationService;
            _logger = logger;
        }

        public async Task<SaleResult> SellAsync(SaleRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var item in request.Items)
                {
                    var allocation = await _allocationService.AllocateAsync(new AllocationRequest
                    {
                        ProductId = item.ProductId,
                        WarehouseId = request.WarehouseId,
                        Quantity = item.Quantity
                    });

                    if (!allocation.Success)
                    {
                        await transaction.RollbackAsync();
                        return new SaleResult
                        {
                            Success = false,
                            Message = $"المخزون غير كافٍ للمنتج رقم {item.ProductId}"
                        };
                    }

                    foreach (var deduction in allocation.Deductions)
                    {
                        _context.StockTransactions.Add(new StockTransaction
                        {
                            LotId = deduction.LotId,
                            Quantity = -deduction.QuantityDeducted,
                            TransactionDate = DateTime.UtcNow,
                            Reference = $"Sale-{DateTime.UtcNow.Ticks}"
                        });
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // إرسال الإشعارات بعد نجاح البيع
                foreach (var item in request.Items)
                {
                    await _notificationService.CheckAndNotifyAsync(item.ProductId, request.WarehouseId);
                }

                return new SaleResult { Success = true, Message = "تم البيع بنجاح" };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "فشل في عملية البيع");
                await transaction.RollbackAsync();
                return new SaleResult { Success = false, Message = "حدث خطأ غير متوقع" };
            }
        }
    }
}