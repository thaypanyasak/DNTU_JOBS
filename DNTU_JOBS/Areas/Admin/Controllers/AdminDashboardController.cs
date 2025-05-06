using DNTU_JOBS.Data;
using DNTU_JOBS.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using DNTU_JOBS.ViewModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace DNTU_JOBS.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles ="Admin")]
    public class AdminDashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AdminDashboardController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _context = db;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        public async Task<IActionResult> Index(string selectedMenuItem)
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                return RedirectToAction("Login", "Account", new { area = "Identity" });
            }

            var admin = await _context.Admins
                .Where(e => e.ApplicationUserId == user.Id)
                .FirstOrDefaultAsync();

            if (admin == null)
            {
                return Unauthorized(); // Nếu không phải admin, không cho phép truy cập
            }

            // Lấy số lượng thông báo chưa đọc
            var unreadJobPostNotifications = await _context.Notifications
                .Where(n => n.UserId == user.Id && !n.IsRead && n.Type == "JobPost")
                .CountAsync();

            var unreadJobEditNotifications = await _context.Notifications
                .Where(n => n.UserId == user.Id && !n.IsRead && n.Type == "JobEdit")
                .CountAsync();

            var unreadChatMessages = await _context.ChatMessages
                .Where(m => m.ReceiverId == user.Id && !m.IsRead)
                .CountAsync();

            ViewBag.UnreadJobPostNotifications = unreadJobPostNotifications;
            ViewBag.UnreadJobEditNotifications = unreadJobEditNotifications;
            ViewBag.UnreadChatMessages = unreadChatMessages;

            var model = new AdminDashboardViewModel
            {
                SelectedMenuItem = selectedMenuItem,
                AdminName = admin?.Name,
                AdminPhoto = admin?.Photo,
                CanManageAccount = admin?.CanManageAccount ?? false,
                CanManageJobs = admin?.CanManageJobs ?? false,
                CanManageChats = admin?.CanManageChats ?? false
            };

            return View(model);
        }





        // Phương thức để đánh dấu tất cả thông báo là đã đọc
        [HttpPost]
        public async Task<IActionResult> MarkJobPostNotificationsAsRead()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized(); // Người dùng chưa đăng nhập
            }

            var notifications = await _context.Notifications
                .Where(n => n.UserId == user.Id && !n.IsRead && n.Type == "JobPost")
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true; // Đánh dấu là đã đọc
            }

            await _context.SaveChangesAsync(); // Lưu thay đổi vào DB

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> MarkJobEditNotificationsAsRead()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized(); // Người dùng chưa đăng nhập
            }

            var notifications = await _context.Notifications
                .Where(n => n.UserId == user.Id && !n.IsRead && n.Type == "JobEdit")
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true; // Đánh dấu là đã đọc
            }

            await _context.SaveChangesAsync(); // Lưu thay đổi vào DB

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> MarkChatMessagesAsRead()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized(); // Người dùng chưa đăng nhập
            }

            var messages = await _context.ChatMessages
                .Where(m => m.ReceiverId == user.Id && !m.IsRead)
                .ToListAsync();

            foreach (var message in messages)
            {
                message.IsRead = true; // Đánh dấu là đã đọc
            }

            await _context.SaveChangesAsync(); // Lưu thay đổi vào DB

            return Json(new { success = true });
        }





        // Đăng xuất
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home", new { area = "User" });
        }
    }
}
