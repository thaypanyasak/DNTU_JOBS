using DNTU_JOBS.Data;
using DNTU_JOBS.Models;
using DNTU_JOBS.ViewModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;


namespace DNTU_JOBS.ViewComponents
{
    public class NewMessageNotificationViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        
        public NewMessageNotificationViewComponent(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            if (user == null)
            {
                return Content(""); // Trả về nội dung rỗng nếu user chưa đăng nhập
            }

            // Truy vấn số tin nhắn chưa đọc
            var notifications = await _context.ChatMessages
                .Where(m => m.ReceiverId == user.Id && !m.IsRead)
                .GroupBy(m => m.SenderId)
                .Select(group => new
                {
                    SenderId = group.Key,
                    SenderName = _context.Users.FirstOrDefault(u => u.Id == group.Key).UserName,
                    UnreadCount = group.Count()
                })
                .ToListAsync();

            // Chuyển đổi kết quả truy vấn sang ViewModel
            var notificationData = notifications.Select(n => new MessageNotificationViewModel
            {
                SenderId = n.SenderId,
                SenderName = n.SenderName,
                UnreadCount = n.UnreadCount
            }).ToList();

            return View(notificationData);
        }


    }

}
