using DNTU_JOBS.Data;
using DNTU_JOBS.Models;
using DNTU_JOBS.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DNTU_JOBS.Areas.User.Controllers
{
    [Area("User")]
    public class UserChatController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserChatController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var unreadMessages = await _context.ChatMessages
            .Where(m => m.ReceiverId == user.Id && !m.IsRead)
            .ToListAsync();

            foreach (var message in unreadMessages)
            {
                message.IsRead = true; // Đánh dấu là đã đọc
            }

            var employersQuery = await _context.ChatMessages
                .Where(m => m.SenderId == user.Id)
                .GroupBy(m => m.ReceiverId)
                .Select(group => new
                {
                    EmployerId = group.Key,
                    Name = _context.Users.FirstOrDefault(u => u.Id == group.Key).Name,
                    Photo = _context.Users.FirstOrDefault(u => u.Id == group.Key).Photo,
                    UnreadMessages = group.Count(m => !m.IsRead && m.ReceiverId == user.Id)
                })
                .ToListAsync();

            var employers = employersQuery.Select(e => new EmployerChatInfo
            {
                EmployerId = e.EmployerId,
                Name = e.Name,
                Photo = e.Photo != null ? ConvertToBase64(e.Photo) : "default-image-url",  // Thêm ảnh mặc định nếu không có
                UnreadMessages = e.UnreadMessages
            }).ToList();

            var model = new UserChatViewModel
            {
                ApplicationUserId = user.Id,
                UserPhoto = ConvertToBase64(user.Photo),
                Employers = employers
            };

            return View(model);
        }

        // Hàm chuyển đổi byte[] sang base64
        private static string? ConvertToBase64(byte[]? photo)
        {
            if (photo == null) return null;
            return $"data:image/jpeg;base64,{Convert.ToBase64String(photo)}";
        }
    }
}
