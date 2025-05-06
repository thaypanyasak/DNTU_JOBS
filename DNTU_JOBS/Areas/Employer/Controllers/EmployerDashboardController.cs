using DNTU_JOBS.Data;
using DNTU_JOBS.Models;
using DNTU_JOBS.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DNTU_JOBS.Areas.Employer.Controllers
{
    [Area("Employer")]
    [Authorize(Roles = "Employer")]
    public class EmployerDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager; // Thêm SignInManager

        public EmployerDashboardController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager; // Khởi tạo SignInManager
        }

        public async Task<IActionResult> Index(string selectedMenuItem)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            var employer = await _context.Employers
                                        .Where(e => e.ApplicationUserId == user.Id)
                                        .FirstOrDefaultAsync();

            // Số thông báo chưa đọc liên quan tới quản lý bài đăng
            var unreadJobPostNotifications = await _context.Notifications
                .Where(n => n.UserId == user.Id && !n.IsRead &&
                           (n.Type == "Approve" || n.Type == "Rejected" || n.Type == "ApproveEdit" || n.Type == "RejectEdit"))
                .CountAsync();



            // Số thông báo chưa đọc liên quan tới quản lý đơn xin việc
            var unreadApplicationNotifications = await _context.Notifications
                .Where(n => n.UserId == user.Id && !n.IsRead && n.Type == "JobApplication")
                .CountAsync();

            var unreadChatMessages = await _context.ChatMessages
                .Where(m => m.ReceiverId == user.Id && !m.IsRead)
                .CountAsync();

            ViewBag.UnreadJobPostNotifications = unreadJobPostNotifications;
            ViewBag.UnreadApplicationNotifications = unreadApplicationNotifications;
            ViewBag.UnreadChatMessages = unreadChatMessages;

            var model = new EmployerDashboardViewModel
            {
                SelectedMenuItem = selectedMenuItem,
                EmployerName = employer?.Name,
                EmployerPhoto = employer?.Photo
            };

            return View(model);
        }

        // Mark "JobPost" notifications as read
        [HttpPost]
        public async Task<IActionResult> MarkJobPostNotificationsAsRead()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized(); // Return Unauthorized if the user is not logged in
            }

            var notifications = await _context.Notifications
                .Where(n => n.UserId == user.Id && !n.IsRead && (n.Type == "Approve" || n.Type == "Rejected" || n.Type == "ApproveEdit" || n.Type == "RejectEdit"))
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true; // Mark as read
            }

            await _context.SaveChangesAsync(); // Save changes to the database

            return Json(new { success = true });
        }

        // Mark "JobApplication" notifications as read
        [HttpPost]
        public async Task<IActionResult> MarkApplicationNotificationsAsRead()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var notifications = await _context.Notifications
                .Where(n => n.UserId == user.Id && !n.IsRead && n.Type == "JobApplication")
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true; // Mark as read
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // Mark "ChatMessages" notifications as read
        [HttpPost]
        public async Task<IActionResult> MarkChatMessagesAsRead()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var messages = await _context.ChatMessages
                .Where(m => m.ReceiverId == user.Id && !m.IsRead)
                .ToListAsync();

            foreach (var message in messages)
            {
                message.IsRead = true; // Mark as read
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }




        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction("Index", "Home", new { area = "User" });
        }
    }
}
