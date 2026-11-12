using Application.IServices;
using Infrastructure.Data;
using Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<StockHub> _hubContext;
        private readonly ILogger<NotificationService> _logger;
        public NotificationService(AppDbContext context, IHubContext<StockHub> hubContext, ILogger<NotificationService> logger)
        {
            _context = context;
            _hubContext = hubContext;
            _logger = logger;
        }

        public async Task CheckAndNotifyAsync(int productId, int warehouseId)
        {
            // حساب الرصيد الحالي
            var totalQty = await _context.Lots
                .Where(l => l.ProductId == productId && l.WarehouseId == warehouseId)
                .SumAsync(l => l.RemainingQuantity);

            // جلب الحد الأدنى
            var threshold = await _context.StockThresholds
                .FirstOrDefaultAsync(t => t.ProductId == productId && t.WarehouseId == warehouseId);

            if (threshold != null && totalQty < threshold.MinQuantity)
            {
                var product = await _context.Products.FindAsync(productId);
                var productName = product?.Name ?? "منتج غير معروف";

                var message = $"⚠️ انخفض مخزون {productName} في المستودع {warehouseId} إلى {totalQty} (الحد: {threshold.MinQuantity})";

                // إرسال إشعار فوري لمجموعة المدراء
                await _hubContext.Clients.Group("Managers").SendAsync("ReceiveAlert", message);

                // تسجيل الإشعار في قاعدة البيانات
                var notification = new Domain.Entities.Notification
                {
                    Message = message,
                    CreatedDate = DateTime.UtcNow,
                    IsRead = false
                };
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
            }
        }
    }
}