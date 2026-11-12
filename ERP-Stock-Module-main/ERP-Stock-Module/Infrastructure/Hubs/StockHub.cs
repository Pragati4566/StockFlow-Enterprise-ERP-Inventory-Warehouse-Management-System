using Microsoft.AspNetCore.SignalR;

namespace Infrastructure.Hubs
{
    public class StockHub : Hub
    {
        // يمكن إضافة دوال للاشتراك في مجموعات (اختياري)
        public async Task JoinWarehouseGroup(int warehouseId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Warehouse-{warehouseId}");
        }
    }
}