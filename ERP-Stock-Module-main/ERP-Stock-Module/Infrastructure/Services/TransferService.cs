using Application.DTOs;
using Application.IServices;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;
using Domain.Enums;
using Microsoft.Extensions.Logging;


namespace Infrastructure.Services
{
    public class TransferService : ITransferService
    {
        private readonly AppDbContext _context;
        private readonly IStockAllocationService _allocationService;
        private readonly INotificationService _notificationService;
        private readonly ILogger<TransferService> _logger;
        public TransferService(AppDbContext context, IStockAllocationService allocationService, INotificationService notificationService, ILogger<TransferService> logger)
        {
            _context = context;
            _allocationService = allocationService;
            _notificationService = notificationService;
            _logger = logger;
        }
        public async Task<List<TransferItem>> GetPendingItemsAsync()
        {
            return await _context.TransferItems
                .Where(ti => ti.Status == TransferStatus.Pending)
                .Include(ti => ti.TransferOrder)
                .Select(ti => new TransferItem
                {
                    Id = ti.Id,
                    TransferOrderId = ti.TransferOrderId,
                    ProductId = ti.ProductId,
                    QuantitySent = ti.QuantitySent,
                    QuantityReceived = ti.QuantityReceived,
                    Status = ti.Status,
                    ReceivedDate = ti.ReceivedDate,
                    // لا نقوم بتضمين TransferOrder.TransferItems هنا
                })
                .ToListAsync();
        }
        // المرحلة الأولى: إرسال النقل

        public async Task<TransferResult> SendTransferAsync(TransferRequest request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // إنشاء أمر النقل
                var order = new TransferOrder
                {
                    SourceWarehouseId = request.SourceWarehouseId,
                    DestinationWarehouseId = request.DestinationWarehouseId,
                    CreatedDate = DateTime.UtcNow,
                    Status = TransferStatus.Pending
                };
                _context.TransferOrders.Add(order);
                await _context.SaveChangesAsync();

                // معالجة كل منتج في النقل
                foreach (var item in request.Items)
                {
                    var allocation = await _allocationService.AllocateAsync(new AllocationRequest
                    {
                        ProductId = item.ProductId,
                        WarehouseId = request.SourceWarehouseId,
                        Quantity = item.Quantity
                    });

                    if (!allocation.Success)
                    {
                        await transaction.RollbackAsync();
                        return new TransferResult { Success = false, Message = $"المخزون غير كافٍ للمنتج {item.ProductId}" };
                    }

                    var transferItem = new TransferItem
                    {
                        TransferOrderId = order.Id,
                        ProductId = item.ProductId,
                        QuantitySent = item.Quantity,
                        Status = TransferStatus.Pending
                    };
                    _context.TransferItems.Add(transferItem);
                    await _context.SaveChangesAsync();

                    foreach (var deduction in allocation.Deductions)
                    {
                        _context.StockTransactions.Add(new StockTransaction
                        {
                            LotId = deduction.LotId,
                            Quantity = -deduction.QuantityDeducted,
                            TransactionDate = DateTime.UtcNow,
                            Reference = $"TransferOut-{order.Id}-{transferItem.Id}"
                        });
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // ✅ الإشعارات بعد الإرسال الناجح
                foreach (var item in request.Items)
                {
                    await _notificationService.CheckAndNotifyAsync(item.ProductId, request.SourceWarehouseId);
                }

                return new TransferResult { Success = true, TransferOrderId = order.Id };
            }
            catch
            {
                await transaction.RollbackAsync();
                return new TransferResult { Success = false, Message = "فشل إرسال النقل" };
            }
        }


        // المرحلة الثانية: تأكيد الاستلام
        public async Task<TransferResult> ConfirmReceiptAsync(int transferItemId, decimal quantityReceived)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var item = await _context.TransferItems
                    .Include(i => i.TransferOrder)
                    .FirstOrDefaultAsync(i => i.Id == transferItemId);

                if (item == null || item.Status != TransferStatus.Pending)
                {
                    await transaction.RollbackAsync();
                    return new TransferResult { Success = false, Message = "بند النقل غير موجود أو تم استلامه مسبقاً" };
                }


                if (quantityReceived != item.QuantitySent)
                {
                    await transaction.RollbackAsync();
                    return new TransferResult { Success = false, Message = "الكمية المستلمة غير مطابقة للكمية المرسلة" };
                }

                // إنشاء دفعة جديدة في المستودع الوجهة
                var newLot = new Lot
                {
                    ProductId = item.ProductId,
                    SupplierId = null,  // النقل لا يحمل مورداً محدداً
                    WarehouseId = item.TransferOrder.DestinationWarehouseId,
                    QuantityReceived = quantityReceived,
                    RemainingQuantity = quantityReceived,
                    UnitCost = 0,       // يمكن تحسينه لاحقاً بحساب متوسط التكلفة
                    ReceivedDate = DateTime.UtcNow
                };
                _context.Lots.Add(newLot);
                await _context.SaveChangesAsync();

                // تسجيل حركة دخول إلى الوجهة
                _context.StockTransactions.Add(new StockTransaction
                {
                    LotId = newLot.Id,
                    Quantity = quantityReceived,
                    TransactionDate = DateTime.UtcNow,
                    Reference = $"TransferIn-{item.TransferOrderId}-{item.Id}"
                });

                // تحديث حالة البند
                item.QuantityReceived = quantityReceived;
                item.Status = TransferStatus.Completed;
                item.ReceivedDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
             
                await _notificationService.CheckAndNotifyAsync(item.ProductId, item.TransferOrder.DestinationWarehouseId);

                return new TransferResult { Success = true, Message = "تم تأكيد الاستلام بنجاح",
                    TransferItemId = transferItemId,
                    QuantityReceived = quantityReceived
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "فشل في عملية النقل");
                await transaction.RollbackAsync();
                return new TransferResult { Success = false, Message = "فشل النقل" };
            }
        }
    }
}