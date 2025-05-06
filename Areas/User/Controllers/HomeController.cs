using DNTU_JOBS.Data;
using DNTU_JOBS.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace DNTU_JOBS.Areas.User.Controllers
{
    [Area("User")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;

        public HomeController(ApplicationDbContext db)
        {
            _db = db;
        }
        public IActionResult Index()
        {
            // Lấy danh sách 4 công việc mới nhất có trạng thái phù hợp
            var jobs = _db.Jobs
                .Where(job => job.Status == ApprovalStatus.Approved ||
                              job.Status == ApprovalStatus.ApprovedEdit ||
                              job.Status == ApprovalStatus.Working)
                .Include(job => job.District) // Load thông tin District
                .Include(job => job.Ward)     // Load thông tin Ward
                .Include(job => job.ApplicationUser) // Load thông tin từ ApplicationUser
                .OrderByDescending(job => job.CreationDate)
                .Take(4)
                .ToList();

            return View(jobs); // Truyền danh sách công việc vào View
        }

        public IActionResult Jobs()
        {
            return View();
        }
        public IActionResult JobDetail()
        {
            return View();
        }

        public IActionResult Contact()
        {
            return View();
        }
    }
}
