using Domain.Entities;

namespace Application.IServices
{
    public interface ILotRepository
    {
        Task<List<Lot>> GetAvailableLotsAsync(int productId, int warehouseId);
        Task<bool> TryDeductQuantityAsync(int lotId, decimal quantity); 
    }
}