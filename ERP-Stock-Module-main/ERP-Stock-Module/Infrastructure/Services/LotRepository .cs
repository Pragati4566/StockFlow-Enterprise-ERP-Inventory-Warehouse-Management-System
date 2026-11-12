using Application.IServices;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Domain.Entities;

namespace Infrastructure.Services
{
    public class LotRepository : ILotRepository
    {
        private readonly AppDbContext _context;

        public LotRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Lot>> GetAvailableLotsAsync(int productId, int warehouseId)
        {
            return await _context.Lots
                .FromSqlRaw(@"
                    SELECT * FROM Lots WITH (UPDLOCK, HOLDLOCK)
                    WHERE ProductId = {0} AND WarehouseId = {1} AND RemainingQuantity > 0
                    ORDER BY ReceivedDate OFFSET 0 ROWS", productId, warehouseId)
                .ToListAsync();
        }

        public async Task<bool> TryDeductQuantityAsync(int lotId, decimal quantity)
        {
            var rowsAffected = await _context.Database.ExecuteSqlRawAsync(
                "UPDATE Lots SET RemainingQuantity = RemainingQuantity - {0} WHERE Id = {1} AND RemainingQuantity >= {0}",
                quantity, lotId);
            return rowsAffected > 0;
        }
    }
}