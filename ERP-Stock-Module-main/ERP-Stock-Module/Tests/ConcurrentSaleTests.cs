using Application.DTOs;
using Application.IServices;
using Infrastructure.Data;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.SignalR;

namespace Tests
{
    public class ConcurrentSaleTests
    {
        [Fact]
        public async Task RealConcurrentSale_ShouldPreventOverselling()
        {
            // ============================================================
            // 1. نصنع قاعدة بيانات SQL Server واحدة (باسم ثابت) للجميع
            // ============================================================
            string dbName = $"ConcurrentTest_{Guid.NewGuid():N}";
            string connectionString = $"Server=(localdb)\\MSSQLLocalDB;Database={dbName};Integrated Security=True;";

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            // ============================================================
            // 2. إضافة البيانات الأولية (نفس قاعدة البيانات)
            // ============================================================
            // نستخدم using عشان نحرر السياق بعد ما نضيف البيانات
            using (var setupContext = new AppDbContext(options))
            {
                setupContext.Database.EnsureCreated(); // يسوي الجداول

                var product = new Product { Name = "زيت" };
                var warehouse = new Warehouse { Name = "مستودع" };
                var supplier = new Supplier { Name = "مورد" };

                setupContext.Products.Add(product);
                setupContext.Warehouses.Add(warehouse);
                setupContext.Suppliers.Add(supplier);
                await setupContext.SaveChangesAsync();

                var lot = new Lot
                {
                    ProductId = product.Id,
                    WarehouseId = warehouse.Id,
                    SupplierId = supplier.Id,
                    QuantityReceived = 1,
                    RemainingQuantity = 1,  // قطعة واحدة فقط
                    UnitCost = 10,
                    ReceivedDate = DateTime.UtcNow
                };
                setupContext.Lots.Add(lot);
                await setupContext.SaveChangesAsync();
            } // هنا السياق setupContext ينقفل وينحذف، لكن البيانات فضلت في قاعدة البيانات.

            // ============================================================
            // 3. نصنع سياقين جديدين، لكن يشتركان في نفس قاعدة البيانات (نفس الـ options)
            // ============================================================
            // هذا هو السر: السياقين منفصلين (آمنين للخيوط) لكن بياناتهم واحدة.
            using var contextForTask1 = new AppDbContext(options);
            using var contextForTask2 = new AppDbContext(options);

            // ============================================================
            // 4. نبني الخدمات (كل خدمة تأخذ السياق الخاص فيها)
            // ============================================================
            // الخدمة 1
            var lotRepo1 = new LotRepository(contextForTask1);
            var allocationService1 = new StockAllocationService(lotRepo1, NullLogger<StockAllocationService>.Instance);
            var notificationService1 = new NotificationService(contextForTask1, new MockHubContext(), NullLogger<NotificationService>.Instance);
            var sellService1 = new SellService(contextForTask1, allocationService1, notificationService1, NullLogger<SellService>.Instance);

            // الخدمة 2
            var lotRepo2 = new LotRepository(contextForTask2);
            var allocationService2 = new StockAllocationService(lotRepo2, NullLogger<StockAllocationService>.Instance);
            var notificationService2 = new NotificationService(contextForTask2, new MockHubContext(), NullLogger<NotificationService>.Instance);
            var sellService2 = new SellService(contextForTask2, allocationService2, notificationService2, NullLogger<SellService>.Instance);

            // ============================================================
            // 5. تجهيز طلب البيع
            // ============================================================
            // هون لازم تجيب الـ Id حق المستودع من قاعدة البيانات.
            // لسهولتها، بدنا نجيب الـ Warehouse Id من قاعدة البيانات نفسها.
            int warehouseId;
            int productId;
            int lotId;
            using (var tempContext = new AppDbContext(options))
            {
                var wh = await tempContext.Warehouses.FirstAsync();
                var p = await tempContext.Products.FirstAsync();
                var l = await tempContext.Lots.FirstAsync();
                warehouseId = wh.Id;
                productId = p.Id;
                lotId = l.Id;
            }

            var request = new SaleRequest
            {
                WarehouseId = warehouseId,
                Items = new List<SaleItem> { new SaleItem { ProductId = productId, Quantity = 1 } }
            };

            // ============================================================
            // 6. تنفيذ عمليتي البيع في نفس الوقت (متزامن)
            // ============================================================
            var task1 = sellService1.SellAsync(request);
            var task2 = sellService2.SellAsync(request);

            var results = await Task.WhenAll(task1, task2);

            // ============================================================
            // 7. التأكد من النتائج
            // ============================================================
            int successCount = results.Count(r => r.Success);
            int failCount = results.Count(r => !r.Success);

            // نتوقع نجاح واحد وفشل واحد (لأن الكمية كانت 1)
            Assert.Equal(1, successCount);
            Assert.Equal(1, failCount);

            // نتأكد أن المخزون أصبح صفر
            using (var checkContext = new AppDbContext(options))
            {
                var updatedLot = await checkContext.Lots.FindAsync(lotId);
                Assert.Equal(0, updatedLot.RemainingQuantity);
            }
        }

        // هذه فئة وهمية (Mock) عشان ترضي SignalR، تجاهلها.
        private class MockHubContext : Microsoft.AspNetCore.SignalR.IHubContext<Infrastructure.Hubs.StockHub>
        {
            public Microsoft.AspNetCore.SignalR.IHubClients Clients => null!;
            public Microsoft.AspNetCore.SignalR.IGroupManager Groups => null!;
        }
    }
}