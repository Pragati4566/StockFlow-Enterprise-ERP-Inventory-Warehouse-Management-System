using Application.DTOs;
using Application.IServices;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services
{
    public class StockAllocationService : IStockAllocationService
    {
        private readonly ILotRepository _lotRepository;
        private readonly ILogger<StockAllocationService> _logger;

        public StockAllocationService(ILotRepository lotRepository, ILogger<StockAllocationService> logger)
        {
            _lotRepository = lotRepository;
            _logger = logger;
        }

        public async Task<AllocationResult> AllocateAsync(AllocationRequest request)
        {
            var lots = await _lotRepository.GetAvailableLotsAsync(request.ProductId, request.WarehouseId);

            var totalAvailable = lots.Sum(l => l.RemainingQuantity);
            if (totalAvailable < request.Quantity)
                return new AllocationResult { Success = false, Message = "المخزون غير كافٍ" };

            var remaining = request.Quantity;
            var deductions = new List<LotDeduction>();

            foreach (var lot in lots)
            {
                if (remaining <= 0) break;

                var deduct = Math.Min(remaining, lot.RemainingQuantity);

                // استخدام الخصم الذري بدلاً من التعديل في الذاكرة
                if (!await _lotRepository.TryDeductQuantityAsync(lot.Id, deduct))
                {
                    // فشل الخصم لأن كمية أخرى خُصِمت في نفس اللحظة
                    return new AllocationResult { Success = false, Message = "المخزون غير كافٍ" };
                }

                deductions.Add(new LotDeduction
                {
                    LotId = lot.Id,
                    QuantityDeducted = deduct,
                    UnitCost = lot.UnitCost
                });

                remaining -= deduct;
            }

            return new AllocationResult { Success = true, Deductions = deductions };
        }
    }
}