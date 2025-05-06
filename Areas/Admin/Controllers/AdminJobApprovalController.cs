using DNTU_JOBS.Data;
using DNTU_JOBS.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DNTU_JOBS.ViewModel;
using DNTU_JOBS.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace DNTU_JOBS.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "CanManageJobPolicy")]
    public class AdminJobApprovalController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<NotificationHub> _notificationHub;

        public AdminJobApprovalController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IHubContext<NotificationHub> notificationHub)
        {
            _context = context;
            _userManager = userManager;
            _notificationHub = notificationHub;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            // Get all users who are Employers
            var allUsers = await _userManager.GetUsersInRoleAsync("Employer");

            var usersWithJobStatuses = new List<object>();

            foreach (var user in allUsers)
            {
                var jobStatuses = await _context.Jobs
                    .Where(j => j.ApplicationUserId == user.Id)
                    .ToListAsync();

                if (jobStatuses.Any())
                {
                    var pendingJobCount = jobStatuses.Count(j => j.Status == ApprovalStatus.Pending);
                    var approvedJobCount = jobStatuses.Count(j => j.Status == ApprovalStatus.Approved);
                    var rejectedJobCount = jobStatuses.Count(j => j.Status == ApprovalStatus.Rejected);
                    var pendingEditJobCount = jobStatuses.Count(j => j.Status == ApprovalStatus.PendingEdit);
                    var approvedEditJobCount = jobStatuses.Count(j => j.Status == ApprovalStatus.ApprovedEdit);
                    var rejectedEditJobCount = jobStatuses.Count(j => j.Status == ApprovalStatus.RejectedEdit);
                    var filledJobCount = jobStatuses.Count(j => j.Status == ApprovalStatus.Filled);

                    var totalJobCount = jobStatuses.Count;

                    usersWithJobStatuses.Add(new
                    {
                        User = user,
                        PendingJobCount = pendingJobCount,
                        ApprovedJobCount = approvedJobCount,
                        RejectedJobCount = rejectedJobCount,
                        PendingEditJobCount = pendingEditJobCount,
                        ApprovedEditJobCount = approvedEditJobCount,
                        RejectedEditJobCount = rejectedEditJobCount,
                        FilledJobCount = filledJobCount,
                        TotalJobCount = totalJobCount
                    });
                }

                // Đánh dấu các thông báo loại "JobPost" của người dùng là đã đọc
                var unreadJobPostNotifications = await _context.Notifications
                    .Where(n => n.UserId == user.Id && !n.IsRead && n.Type == "JobPost")
                    .ToListAsync();

                foreach (var notification in unreadJobPostNotifications)
                {
                    notification.IsRead = true;
                }

                // Lưu lại các thay đổi
                await _context.SaveChangesAsync();
            }

            // Tổng số user
            var totalUsers = usersWithJobStatuses.Count;

            // Tính tổng số trang
            int totalPages = (int)Math.Ceiling(totalUsers / (double)pageSize);

            // Chia user theo phân trang
            var paginatedUsers = usersWithJobStatuses
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Truyền thông tin phân trang vào ViewBag
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;

            return View(paginatedUsers);
        }






        public async Task<IActionResult> JobsByUser(string userId, int page = 1, int pageSize = 10)
        {
            var userJobs = await _context.Jobs
                .Where(j => j.ApplicationUserId == userId)
                .Include(j => j.ApplicationUser)
                .Include(j => j.District)
                .Include(j => j.Ward)
                .Include(j => j.JobApplications) // Bao gồm JobApplications để đếm số lượng ứng tuyển
                .ToListAsync();

            if (userJobs.Count == 0)
            {
                return NotFound();
            }

            // Tổng số công việc
            var totalJobs = userJobs.Count;

            // Tính tổng số trang
            int totalPages = (int)Math.Ceiling(totalJobs / (double)pageSize);

            // Chia công việc theo phân trang
            var paginatedJobs = userJobs
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Truyền thông tin phân trang vào ViewBag
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;

            return View(paginatedJobs);
        }



        public async Task<IActionResult> UpdateJobStatusIfFilled()
        {
            var jobs = await _context.Jobs
                .Where(j => j.HiringQuantity <= 0 && j.Status == ApprovalStatus.Approved)
                .ToListAsync();

            foreach (var job in jobs)
            {
                job.Status = ApprovalStatus.Filled; 
                job.IsActive = false; 
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }


        [HttpPost]
        public async Task<IActionResult> ApproveJob(int id)
        {
            var job = await _context.Jobs
                .Include(j => j.ApplicationUser)
                .Include(j => j.JobCategory) // Bao gồm danh mục công việc
                .FirstOrDefaultAsync(j => j.Id == id);

            if (job == null)
            {
                return NotFound();
            }

            // Cập nhật trạng thái công việc
            job.Status = ApprovalStatus.Approved;
            job.IsActive = true;
            await _context.SaveChangesAsync();

            // Thông báo tới chủ bài đăng
            var notificationForOwner = new Notification
            {
                UserId = job.ApplicationUser.Id,
                Message = $"Công việc '{job.JobTitle}' đã được duyệt!",
                CreatedAt = DateTime.Now,
                IsRead = false,
                Type = "Approve"
            };
            _context.Notifications.Add(notificationForOwner);

            // Lấy tất cả người dùng đã chọn danh mục này
            var interestedUsers = await _context.UserJobCategories
                .Where(ujc => ujc.JobCategoryId == job.JobCategory.Id
                              && ujc.UserTable.ApplicationUserId != job.ApplicationUser.Id) // Loại trừ chủ bài đăng
                .Include(ujc => ujc.UserTable)
                .ToListAsync();

            foreach (var userCategory in interestedUsers)
            {
                var user = userCategory.UserTable;

                // Tạo thông báo cho từng người dùng
                var notificationForUser = new Notification
                {
                    UserId = user.ApplicationUserId,
                    Message = $"Công việc mới '{job.JobTitle}' phù hợp với danh mục bạn đã chọn!",
                    CreatedAt = DateTime.Now,
                    IsRead = false,
                    Type = "JobCategoryMatch"
                };
                _context.Notifications.Add(notificationForUser);

                // Gửi thông báo qua SignalR
                await _notificationHub.Clients.User(user.ApplicationUserId).SendAsync("ReceiveNotification", notificationForUser.Message);
            }

            // Lưu tất cả thông báo vào cơ sở dữ liệu
            await _context.SaveChangesAsync();

            return RedirectToAction("JobsByUser", new { userId = job.ApplicationUser.Id });
        }



        



        [HttpPost]
        public async Task<IActionResult> RejectJob(int id, string reason)
        {
            var job = await _context.Jobs.Include(j => j.ApplicationUser).FirstOrDefaultAsync(j => j.Id == id);
            if (job == null)
            {
                return NotFound();
            }

            job.Status = ApprovalStatus.Rejected;
            job.IsActive = false;
            job.RejectionReason = reason; // Lưu lý do từ chối vào cơ sở dữ liệu
            await _context.SaveChangesAsync();

            // Tạo thông báo cho nhà tuyển dụng
            var notification = new Notification
            {
                UserId = job.ApplicationUser.Id,
                Message = $"Công việc '{job.JobTitle}' đã bị từ chối lý do: {reason}",
                CreatedAt = DateTime.Now,
                IsRead = false,
                Type = "Rejected"
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Gửi thông báo qua SignalR
            await _notificationHub.Clients.User(job.ApplicationUser.Id).SendAsync("ReceiveNotification", notification.Message);

            return RedirectToAction("JobsByUser", new { userId = job.ApplicationUserId });
        }


        [HttpPost]
        public async Task<IActionResult> ToggleJobStatus(int id, string reason = null)
        {
            var job = await _context.Jobs
                .Include(j => j.ApplicationUser) // Include thông tin Employer
                .FirstOrDefaultAsync(j => j.Id == id);

            if (job == null)
            {
                return Json(new { success = false, message = "Không tìm thấy công việc." });
            }

            job.IsActive = job.IsActive ?? false; 
            job.IsActive = !job.IsActive.Value;

            if (!job.IsActive.Value)
            {
                job.Status = ApprovalStatus.Break; // Chuyển trạng thái thành Break
                job.RejectionReason = reason; // Lưu lý do tạm ngừng
            }
            else
            {
                job.Status = ApprovalStatus.Working; // Chuyển trạng thái thành Working
                job.RejectionReason = null; // Xóa lý do tạm ngừng nếu kích hoạt lại
            }

            await _context.SaveChangesAsync();

            // Tạo thông báo
            var message = job.IsActive.Value
                ? $"Công việc '{job.JobTitle}' của bạn đã được hoạt động bình thường."
                : $"Công việc '{job.JobTitle}' của bạn đã bị tạm ngừng. Lý do: {reason}";

            var notification = new Notification
            {
                UserId = job.ApplicationUser.Id,
                Message = message,
                CreatedAt = DateTime.Now,
                IsRead = false,
                Type = "JobStatus"
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Gửi thông báo qua SignalR
            await _notificationHub.Clients.User(job.ApplicationUser.Id).SendAsync("ReceiveNotification", message);

            return Json(new { success = true, isActive = job.IsActive, reason });
        }





        public async Task<IActionResult> JobDetail(int id)
        {
            var job = await _context.Jobs
                .Include(j => j.ApplicationUser)
                .Include(j => j.District)
                .Include(j => j.Ward)
                .Include(j => j.JobCategory)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (job == null)
            {
                return NotFound();
            }

            // Add the number of applicants to the job model
            var numberOfApplicants = await _context.JobApplications
                .Where(a => a.JobId == id)
                .CountAsync();

            // You can pass the number of applicants along with the job
            ViewBag.NumberOfApplicants = numberOfApplicants;

            return View(job);
        }




    }
}
