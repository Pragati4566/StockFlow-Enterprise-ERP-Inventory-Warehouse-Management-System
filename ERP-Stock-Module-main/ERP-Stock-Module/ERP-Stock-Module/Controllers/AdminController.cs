using Microsoft.AspNetCore.Mvc;
using Domain.Entities;
using Application.DTOs;
using Application.IServices;

namespace ERP_Stock_Module.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly IRepository<Product> _productRepo;
        private readonly IRepository<Supplier> _supplierRepo;
        private readonly IRepository<Warehouse> _warehouseRepo;
        private readonly IRepository<StockThreshold> _thresholdRepo;

        public AdminController(
            IRepository<Product> productRepo,
            IRepository<Supplier> supplierRepo,
            IRepository<Warehouse> warehouseRepo,
            IRepository<StockThreshold> thresholdRepo)
        {
            _productRepo = productRepo;
            _supplierRepo = supplierRepo;
            _warehouseRepo = warehouseRepo;
            _thresholdRepo = thresholdRepo;
        }

        // =================== المنتجات ===================
        [HttpGet("products/{id}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            var product = await _productRepo.GetByIdAsync(id);
            if (product == null) return NotFound();
            return Ok(product);
        }

        [HttpGet("products")]
        public async Task<IActionResult> GetProducts() => Ok(await _productRepo.GetAllAsync());

        [HttpPost("products")]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
        {
            var product = await _productRepo.AddAsync(new Product { Name = request.Name, Category= request.Category  });
            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
        }

        [HttpPut("products/{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductRequest request)
        {
            var product = await _productRepo.GetByIdAsync(id);
            if (product == null) return NotFound();
            product.Name = request.Name;
            product.Category = request.Category;
            await _productRepo.UpdateAsync(product);
            return Ok(product);
        }

        [HttpDelete("products/{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _productRepo.GetByIdAsync(id);
            if (product == null) return NotFound();
            await _productRepo.SoftDeleteAsync(product);
            return NoContent();
        }

        // =================== الموردين ===================
        [HttpGet("suppliers/{id}")]
        public async Task<IActionResult> GetSupplier(int id)
        {
            var supplier = await _supplierRepo.GetByIdAsync(id);
            if (supplier == null) return NotFound();
            return Ok(supplier);
        }

        [HttpGet("suppliers")]
        public async Task<IActionResult> GetSuppliers() => Ok(await _supplierRepo.GetAllAsync());

        [HttpPost("suppliers")]
        public async Task<IActionResult> CreateSupplier([FromBody] CreateSupplierRequest request)
        {
            var supplier = await _supplierRepo.AddAsync(new Supplier { Name = request.Name });
            return CreatedAtAction(nameof(GetSupplier), new { id = supplier.Id }, supplier);
        }

        [HttpPut("suppliers/{id}")]
        public async Task<IActionResult> UpdateSupplier(int id, [FromBody] UpdateSupplierRequest request)
        {
            var supplier = await _supplierRepo.GetByIdAsync(id);
            if (supplier == null) return NotFound();
            supplier.Name = request.Name;
            await _supplierRepo.UpdateAsync(supplier);
            return Ok(supplier);
        }

        [HttpDelete("suppliers/{id}")]
        public async Task<IActionResult> DeleteSupplier(int id)
        {
            var supplier = await _supplierRepo.GetByIdAsync(id);
            if (supplier == null) return NotFound();
            await _supplierRepo.SoftDeleteAsync(supplier);
            return NoContent();
        }

        // =================== المستودعات ===================
        [HttpGet("warehouses/{id}")]
        public async Task<IActionResult> GetWarehouse(int id)
        {
            var warehouse = await _warehouseRepo.GetByIdAsync(id);
            if (warehouse == null) return NotFound();
            return Ok(warehouse);
        }

        [HttpGet("warehouses")]
        public async Task<IActionResult> GetWarehouses() => Ok(await _warehouseRepo.GetAllAsync());

        [HttpPost("warehouses")]
        public async Task<IActionResult> CreateWarehouse([FromBody] CreateWarehouseRequest request)
        {
            var warehouse = await _warehouseRepo.AddAsync(new Warehouse { Name = request.Name, Location = request.Location });
            return CreatedAtAction(nameof(GetWarehouse), new { id = warehouse.Id }, warehouse);
        }

        [HttpPut("warehouses/{id}")]
        public async Task<IActionResult> UpdateWarehouse(int id, [FromBody] UpdateWarehouseRequest request)
        {
            var warehouse = await _warehouseRepo.GetByIdAsync(id);
            if (warehouse == null) return NotFound();
            warehouse.Name = request.Name;
            warehouse.Location = request.Location;
            await _warehouseRepo.UpdateAsync(warehouse);
            return Ok(warehouse);
        }

        [HttpDelete("warehouses/{id}")]
        public async Task<IActionResult> DeleteWarehouse(int id)
        {
            var warehouse = await _warehouseRepo.GetByIdAsync(id);
            if (warehouse == null) return NotFound();
            await _warehouseRepo.SoftDeleteAsync(warehouse);
            return NoContent();
        }

        // =================== حدود المخزون الدنيا ===================
        [HttpGet("thresholds")]
        public async Task<IActionResult> GetThresholds()
        {
            var thresholds = await _thresholdRepo.GetAllAsync();
            var result = thresholds.Select(t => new {
                t.Id,
                t.ProductId,
                t.WarehouseId,
                t.MinQuantity
            });
            return Ok(result);
        }

        [HttpPost("thresholds")]
        public async Task<IActionResult> SetThreshold([FromBody] SetThresholdRequest request)
        {
            var existing = (await _thresholdRepo.GetAllAsync(t =>
                t.ProductId == request.ProductId &&
                t.WarehouseId == request.WarehouseId)).FirstOrDefault();

            if (existing != null)
            {
                existing.MinQuantity = request.MinQuantity;
                await _thresholdRepo.UpdateAsync(existing);
            }
            else
            {
                await _thresholdRepo.AddAsync(new StockThreshold
                {
                    ProductId = request.ProductId,
                    WarehouseId = request.WarehouseId,
                    MinQuantity = request.MinQuantity
                });
            }

            return Ok(new { message = "تم حفظ الحد الأدنى بنجاح" });
        }
    }
}