using Microsoft.AspNetCore.Mvc;
using Application.DTOs;
using Application.IServices;

namespace ERP_Stock_Module.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SalesController : ControllerBase
    {
        private readonly ISellService _sellService;

        public SalesController(ISellService sellService)
        {
            _sellService = sellService;
        }

        /// <summary>
        /// تنفيذ عملية بيع جديدة
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Sell([FromBody] SaleRequest request)
        {
            var result = await _sellService.SellAsync(request);

            if (result.Success)
                return Ok(result);
            else
                return BadRequest(result);
        }
    }
}