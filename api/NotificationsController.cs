using DNTU_JOBS.Data;
using DNTU_JOBS.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DNTU_JOBS.api
{
    [Route("api/[controller]")]
    [ApiController]
    public class NotificationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }




        [HttpPost("MarkAllAsRead")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == user.Id && !n.IsRead)
                .ToListAsync();

            unreadNotifications.ForEach(n => n.IsRead = true);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpPost("MarkAllAsReadDB")]
        public async Task<IActionResult> MarkAllAsReadDB()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized(new { success = false, message = "User not authorized" });
            }

            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == user.Id && !n.IsRead)
                .ToListAsync();

            unreadNotifications.ForEach(n => n.IsRead = true);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Notifications marked as read" });
        }



        [HttpDelete]
        [Route("deleteNotification/{id}")]
        public async Task<IActionResult> DeleteNotification(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null)
            {
                return NotFound();
            }

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpGet("GetCurrentUserRole")]
        public async Task<IActionResult> GetCurrentUserRole()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    return Unauthorized();
                }

                var roles = await _userManager.GetRolesAsync(user);
                if (roles == null || !roles.Any())
                {
                    return NotFound(new { error = "No roles found" });
                }

                return Ok(new { Role = roles.First() });
            }
            catch (Exception ex)
            {
                // Log lỗi chi tiết
                Console.WriteLine($"Error in GetCurrentUserRole: {ex.Message}");
                return StatusCode(500, new { error = "Internal server error" });
            }
        }




    }
}
