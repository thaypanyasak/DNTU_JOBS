using DNTU_JOBS.Data;
using DNTU_JOBS.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace DNTU_JOBS.ViewComponents
{
    public class NotificationViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private const int ItemsPerPage = 4; // Number of items per page

        public NotificationViewComponent(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync(int page = 1)
        {
            var user = await _userManager.GetUserAsync(HttpContext.User);
            if (user == null)
            {
                return Content("");
            }

            var notifications = await _context.Notifications
                .Where(n => n.UserId == user.Id)
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * ItemsPerPage)
                .Take(ItemsPerPage)
                .ToListAsync();

            var totalItems = await _context.Notifications.CountAsync(n => n.UserId == user.Id);
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / ItemsPerPage);
            ViewBag.CurrentPage = page;

            return View("Default", notifications);
        }

    }
}
