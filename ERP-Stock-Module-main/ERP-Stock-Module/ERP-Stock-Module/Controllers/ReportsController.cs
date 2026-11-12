using Microsoft.AspNetCore.Mvc;
using Application.DTOs;
using Application.IServices;

namespace ERP_Stock_Module.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportingService _reportingService;

        public ReportsController(IReportingService reportingService)
        {
            _reportingService = reportingService;
        }

        /// <summary>
        /// تقرير حركات المخزون مع التصفية والصفحات
        /// </summary>
        [HttpGet("movements")]
        public async Task<IActionResult> GetMovements([FromQuery] ReportFilter filter)
        {
            var result = await _reportingService.GetMovementsAsync(filter);
            return Ok(result);
        }

        /// <summary>
        /// ملخص المخزون المجمع
        /// </summary>
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary([FromQuery] ReportFilter filter)
        {
            var result = await _reportingService.GetSummaryAsync(filter);
            return Ok(result);
        }
    }
}