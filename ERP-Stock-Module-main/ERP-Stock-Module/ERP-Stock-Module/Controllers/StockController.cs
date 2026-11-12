using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;
using Domain.Entities;
using Application.DTOs;
using Application.IServices;

namespace ERP_Stock_Module.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StockController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;

        public StockController(AppDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        /// <summary>
        /// عرض الأرصدة الحالية للمنتجات في المستودعات
        /// </summary>
        [HttpGet("levels")]
        public async Task<IActionResult> GetStockLevels([FromQuery] int? warehouseId)
        {
            var query = _context.Lots
                .Where(l => !l.IsDeleted && l.RemainingQuantity > 0)
                .AsQueryable();

            if (warehouseId.HasValue)
                query = query.Where(l => l.WarehouseId == warehouseId.Value);

            var levels = await query
     .GroupBy(l => new {
         l.ProductId,
         ProductName = l.Product.Name,    
         l.WarehouseId,
         WarehouseName = l.Warehouse.Name   
     })
     .Select(g => new
     {
         ProductId = g.Key.ProductId,
         ProductName = g.Key.ProductName,
         WarehouseId = g.Key.WarehouseId,
         WarehouseName = g.Key.WarehouseName,
         TotalQuantity = g.Sum(l => l.RemainingQuantity)
     })
     .ToListAsync();

            return Ok(levels);
        }

        /// <summary>
        /// استلام دفعة جديدة (شحنة) من مورد
        /// </summary>
        [HttpPost("receive")]
        public async Task<IActionResult> ReceiveStock([FromBody] ReceiveStockRequest request)
        {
            var lot = new Lot
            {
                ProductId = request.ProductId,
                SupplierId = request.SupplierId,
                WarehouseId = request.WarehouseId,
                QuantityReceived = request.Quantity,
                RemainingQuantity = request.Quantity,
                UnitCost = request.UnitCost,
                ReceivedDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.Lots.Add(lot);

            // تسجيل حركة دخول
            _context.StockTransactions.Add(new StockTransaction
            {
                Lot = lot,                     // سنربطها بعد SaveChanges
                Quantity = request.Quantity,
                TransactionDate = DateTime.UtcNow,
                Reference = $"Purchase-{DateTime.UtcNow.Ticks}"
            });

            await _context.SaveChangesAsync();

            // ربط الحركة بالدفعة بعد حفظها (لأن Id أصبح موجوداً)
            var transaction = await _context.StockTransactions
                .FirstOrDefaultAsync(t => t.Reference == $"Purchase-{DateTime.UtcNow.Ticks}");
            if (transaction != null)
                transaction.LotId = lot.Id; // تأكيد الربط (لكن EF يفعلها تلقائياً إذا أضفنا Lot مباشرة)

            await _context.SaveChangesAsync();

            // ✅ ربط المشكلة الرابعة: فحص إذا كان هذا المنتج لا يزال أقل من الحد الأدنى
            await _notificationService.CheckAndNotifyAsync(request.ProductId, request.WarehouseId);

            return CreatedAtAction(nameof(GetStockLevels), new { warehouseId = request.WarehouseId }, lot);
        }
    }
}