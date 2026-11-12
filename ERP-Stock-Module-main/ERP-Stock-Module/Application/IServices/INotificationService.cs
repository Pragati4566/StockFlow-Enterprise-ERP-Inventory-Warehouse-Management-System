namespace Application.IServices
{
    public interface INotificationService
    {
        Task CheckAndNotifyAsync(int productId, int warehouseId);
    }
}