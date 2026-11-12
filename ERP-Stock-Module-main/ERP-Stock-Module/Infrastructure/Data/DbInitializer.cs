using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            // منع تكرار إضافة البيانات
            if (await context.Warehouses.AnyAsync())
                return;

            // إضافة المستودعات
            var warehouses = new List<Warehouse>
            {
                new() { Name = "الرياض", Location = "السعودية" },
                new() { Name = "جدة", Location = "السعودية" },
                new() { Name = "الدمام", Location = "السعودية" }
            };
            await context.Warehouses.AddRangeAsync(warehouses);
            await context.SaveChangesAsync();

            // إضافة الموردين
            var suppliers = new List<Supplier>
            {
                new() { Name = "شركة النور" },
                new() { Name = "شركة الأصالة" },
                new() { Name = "مؤسسة الصفا" }
            };
            await context.Suppliers.AddRangeAsync(suppliers);
            await context.SaveChangesAsync();

            // إضافة المنتجات
            var products = new List<Product>
{
    new() { Name = "زيت زيتون", Category = "زيوت" },
    new() { Name = "سكر", Category = "مواد غذائية" },
    new() { Name = "أرز", Category = "مواد غذائية" },
    new() { Name = "صابون", Category = "منظفات" },
    new() { Name = "شاي", Category = "مشروبات" }
};
            await context.Products.AddRangeAsync(products);
            await context.SaveChangesAsync();

            // إضافة الدفعات الأولية (Lots)
            var lots = new List<Lot>
            {
                new() { ProductId = 1, SupplierId = 1, WarehouseId = 1, QuantityReceived = 100, RemainingQuantity = 80, UnitCost = 10, ReceivedDate = DateTime.UtcNow.AddMonths(-2) },
                new() { ProductId = 1, SupplierId = 2, WarehouseId = 1, QuantityReceived = 50, RemainingQuantity = 50, UnitCost = 12, ReceivedDate = DateTime.UtcNow.AddMonths(-1) },
                new() { ProductId = 2, SupplierId = 1, WarehouseId = 1, QuantityReceived = 200, RemainingQuantity = 150, UnitCost = 5, ReceivedDate = DateTime.UtcNow.AddMonths(-2) },
                new() { ProductId = 2, SupplierId = 3, WarehouseId = 2, QuantityReceived = 100, RemainingQuantity = 100, UnitCost = 6, ReceivedDate = DateTime.UtcNow.AddMonths(-1) },
                new() { ProductId = 3, SupplierId = 2, WarehouseId = 1, QuantityReceived = 300, RemainingQuantity = 200, UnitCost = 8, ReceivedDate = DateTime.UtcNow.AddMonths(-2) },
                new() { ProductId = 4, SupplierId = 3, WarehouseId = 2, QuantityReceived = 500, RemainingQuantity = 400, UnitCost = 3, ReceivedDate = DateTime.UtcNow.AddMonths(-3) },
                new() { ProductId = 5, SupplierId = 1, WarehouseId = 3, QuantityReceived = 150, RemainingQuantity = 120, UnitCost = 15, ReceivedDate = DateTime.UtcNow.AddMonths(-2) }
            };
            await context.Lots.AddRangeAsync(lots);
            await context.SaveChangesAsync();

            // إضافة حدود المخزون الدنيا
            var thresholds = new List<StockThreshold>
            {
                new() { ProductId = 1, WarehouseId = 1, MinQuantity = 10 },
                new() { ProductId = 2, WarehouseId = 1, MinQuantity = 20 },
                new() { ProductId = 3, WarehouseId = 1, MinQuantity = 30 }
            };
            await context.StockThresholds.AddRangeAsync(thresholds);
            await context.SaveChangesAsync();

            // إضافة إشعار ترحيبي (اختياري)
            var notification = new Notification
            {
                Message = "تم تجهيز النظام بنجاح. أهلاً بك في وحدة المخزون.",
                CreatedDate = DateTime.UtcNow,
                IsRead = false
            };
            await context.Notifications.AddAsync(notification);
            await context.SaveChangesAsync();
        }
    }
}