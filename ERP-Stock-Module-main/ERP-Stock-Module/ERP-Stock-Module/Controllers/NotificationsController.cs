using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Data;

namespace ERP_Stock_Module.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public NotificationsController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// جلب جميع الإشعارات المسجلة
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var notifications = await _context.Notifications
                .OrderByDescending(n => n.CreatedDate)
                .Take(50)
                .ToListAsync();
            return Ok(notifications);
        }
    }
}