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
    [Authorize]
    public class EmployerChatController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public EmployerChatController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var employer = await _userManager.GetUserAsync(User);
            if (employer == null)
            {
                return Unauthorized();
            }

            var usersQuery = await _context.ChatMessages
                .Where(m => m.ReceiverId == employer.Id)
                .GroupBy(m => m.SenderId)
                .Select(group => new
                {
                    UserId = group.Key,
                    Name = _context.Users.FirstOrDefault(u => u.Id == group.Key).Name,
                    Photo = _context.Users.FirstOrDefault(u => u.Id == group.Key).Photo,
                    UnreadMessages = group.Count(m => !m.IsRead && m.ReceiverId == employer.Id)
                })
                .ToListAsync();

            var users = usersQuery.Select(u => new UserChatInfo
            {
                UserId = u.UserId,
                Name = u.Name,
                Photo = u.Photo != null ? ConvertToBase64(u.Photo) : "default-image-url",  // Thêm ảnh mặc định nếu không có
                UnreadMessages = u.UnreadMessages
            }).ToList();

            var model = new EmployerChatViewModel
            {
                ApplicationUserId = employer.Id,
                EmployerPhoto = ConvertToBase64(employer.Photo),
                Users = users
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
