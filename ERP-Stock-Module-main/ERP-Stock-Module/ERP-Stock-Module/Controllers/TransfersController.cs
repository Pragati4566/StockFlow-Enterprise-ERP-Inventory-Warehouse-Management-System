using Microsoft.AspNetCore.Mvc;
using Application.DTOs;
using Application.IServices;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERP_Stock_Module.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TransfersController : ControllerBase
    {
        private readonly ITransferService _transferService;

        public TransfersController(ITransferService transferService)
        {
            _transferService = transferService;
        }
        [HttpGet("pending")]
        public async Task<IActionResult> GetPendingTransfers()
        {
            var pendingItems = await _transferService.GetPendingItemsAsync();
            return Ok(pendingItems);
        }
        /// <summary>
        /// إرسال نقل جديد بين مستودعين
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SendTransfer([FromBody] TransferRequest request)
        {
            var result = await _transferService.SendTransferAsync(request);

            if (result.Success)
                return Ok(result);
            else
                return BadRequest(result);
        }

        /// <summary>
        /// تأكيد استلام بند محدد من أمر نقل
        /// </summary>
        [HttpPut("items/{transferItemId}/receive")]
        public async Task<IActionResult> ConfirmReceipt(int transferItemId, [FromBody] decimal quantityReceived)
        {
            var result = await _transferService.ConfirmReceiptAsync(transferItemId, quantityReceived);

            if (result.Success)
                return Ok(result);
            else
                return BadRequest(result);
        }
    }
}