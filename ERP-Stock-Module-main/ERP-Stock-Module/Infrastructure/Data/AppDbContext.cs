using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Product> Products { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<Warehouse> Warehouses { get; set; }
        public DbSet<Lot> Lots { get; set; }
        public DbSet<StockTransaction> StockTransactions { get; set; }
        public DbSet<TransferOrder> TransferOrders { get; set; }
        public DbSet<TransferItem> TransferItems { get; set; }
        public DbSet<StockThreshold> StockThresholds { get; set; }
        public DbSet<Notification> Notifications { get; set; }


        protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
        {
            configurationBuilder
                .Properties<decimal>()
                .HavePrecision(18, 2);
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Soft Delete Global Filters
            modelBuilder.Entity<Product>().HasQueryFilter(p => !p.IsDeleted);
            modelBuilder.Entity<Supplier>().HasQueryFilter(s => !s.IsDeleted);
            modelBuilder.Entity<Warehouse>().HasQueryFilter(w => !w.IsDeleted);
            modelBuilder.Entity<Lot>().HasQueryFilter(l => !l.IsDeleted);
            modelBuilder.Entity<StockTransaction>().HasQueryFilter(t => !t.IsDeleted);
            modelBuilder.Entity<TransferOrder>().HasQueryFilter(o => !o.IsDeleted);
            modelBuilder.Entity<TransferItem>().HasQueryFilter(i => !i.IsDeleted);
            modelBuilder.Entity<StockThreshold>().HasQueryFilter(th => !th.IsDeleted);
            modelBuilder.Entity<Notification>().HasQueryFilter(n => !n.IsDeleted);

            // 2. الفهارس (Indexes) مع التعليل
            // أ. تسريع التصفية بالتاريخ في التقارير
            modelBuilder.Entity<StockTransaction>()
                .HasIndex(t => t.TransactionDate)
                .HasDatabaseName("IX_StockTransactions_TransactionDate");

            // ب. تسريع JOIN مع Lots
            modelBuilder.Entity<StockTransaction>()
                .HasIndex(t => t.LotId)
                .HasDatabaseName("IX_StockTransactions_LotId");

            // ج. تسريع البحث عن أرصدة منتج في مستودع
            modelBuilder.Entity<Lot>()
                .HasIndex(l => new { l.ProductId, l.WarehouseId })
                .HasDatabaseName("IX_Lots_ProductId_WarehouseId");

            // د. تسريع تقارير الموردين
            modelBuilder.Entity<Lot>()
                .HasIndex(l => l.SupplierId)
                .HasDatabaseName("IX_Lots_SupplierId");

            // هـ. تسريع جلب بنود أمر نقل
            modelBuilder.Entity<TransferItem>()
                .HasIndex(i => i.TransferOrderId)
                .HasDatabaseName("IX_TransferItems_TransferOrderId");

            // و. تسريع فحص الحدود الدنيا
            modelBuilder.Entity<StockThreshold>()
                .HasIndex(th => new { th.ProductId, th.WarehouseId })
                .HasDatabaseName("IX_StockThresholds_ProductId_WarehouseId");


            // منع الحذف المتتالي من مستودع إلى أوامر النقل (لتجنب المسارات المتعددة)
            modelBuilder.Entity<TransferOrder>()
                .HasOne(o => o.SourceWarehouse)
                .WithMany()
                .HasForeignKey(o => o.SourceWarehouseId)
                .OnDelete(DeleteBehavior.Restrict);  // أو DeleteBehavior.NoAction

            modelBuilder.Entity<TransferOrder>()
                .HasOne(o => o.DestinationWarehouse)
                .WithMany()
                .HasForeignKey(o => o.DestinationWarehouseId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}