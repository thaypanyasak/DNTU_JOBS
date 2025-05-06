using DNTU_JOBS.Data;
using DNTU_JOBS.Hubs;
using DNTU_JOBS.Models;
using DNTU_JOBS.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace DNTU_JOBS.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "CanManageJobPolicy")]
    public class AdminJobEditApprovalController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<NotificationHub> _notificationHub;

        public AdminJobEditApprovalController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IHubContext<NotificationHub> notificationHub)
        {
            _context = context;
            _userManager = userManager;
            _notificationHub = notificationHub;
        }

        public async Task<IActionResult> Index(int page = 1, int pageSize = 10)
        {
            // Get all users who are Employers
            var allUsers = await _userManager.GetUsersInRoleAsync("Employer");

            // Create a list to hold user data with pending job edits
            var usersWithPendingEdits = new List<object>();

            foreach (var user in allUsers)
            {
                // Count pending edits (not processed yet)
                var pendingEditCount = await _context.Jobs.CountAsync(j => j.ApplicationUserId == user.Id &&
                    j.Status == ApprovalStatus.PendingEdit);

                // Count all edits (Pending, Approved, Rejected)
                var totalEditCount = await _context.Jobs.CountAsync(j => j.ApplicationUserId == user.Id &&
                    (j.Status == ApprovalStatus.PendingEdit ||
                     j.Status == ApprovalStatus.ApprovedEdit ||
                     j.Status == ApprovalStatus.RejectedEdit));

                // Add user to the list if they have at least one edited job
                if (totalEditCount > 0)
                {
                    usersWithPendingEdits.Add(new
                    {
                        User = user,
                        PendingEditCount = pendingEditCount,
                        TotalEditCount = totalEditCount
                    });
                }

                // Mark all "JobEdit" notifications as read
                var unreadJobEditNotifications = await _context.Notifications
                    .Where(n => n.UserId == user.Id && !n.IsRead && n.Type == "JobEdit")
                    .ToListAsync();

                foreach (var notification in unreadJobEditNotifications)
                {
                    notification.IsRead = true;  // Mark as read
                }

                // Save changes if there are unread notifications
                if (unreadJobEditNotifications.Any())
                {
                    await _context.SaveChangesAsync();
                }
            }

            // Total number of users with pending edits
            int totalUsers = usersWithPendingEdits.Count;

            // Calculate total pages
            int totalPages = (int)Math.Ceiling(totalUsers / (double)pageSize);

            // Ensure `page` is within the valid range
            if (page < 1) page = 1;
            if (page > totalPages) page = totalPages;

            // Get paginated data
            var paginatedUsers = usersWithPendingEdits
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Pass pagination data to ViewBag
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;

            // Pass paginated data to the view
            return View(paginatedUsers);
        }




        public async Task<IActionResult> EditJobsByUser(string userId, int page = 1, int pageSize = 10)
        {
            var totalEditedJobs = await _context.Jobs
                .Where(j => j.ApplicationUserId == userId &&
                            (j.Status == ApprovalStatus.PendingEdit ||
                             j.Status == ApprovalStatus.ApprovedEdit ||
                             j.Status == ApprovalStatus.RejectedEdit))
                .CountAsync();

            var editedJobs = await _context.Jobs
                .Where(j => j.ApplicationUserId == userId &&
                            (j.Status == ApprovalStatus.PendingEdit ||
                             j.Status == ApprovalStatus.ApprovedEdit ||
                             j.Status == ApprovalStatus.RejectedEdit))
                .Include(j => j.ApplicationUser)
                .Include(j => j.District)
                .Include(j => j.Ward)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            int totalPages = (int)Math.Ceiling(totalEditedJobs / (double)pageSize);

            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;

            return View(editedJobs);
        }








        public async Task<IActionResult> ApproveEditedJob(int id)
        {
            var job = await _context.Jobs.Include(j => j.ApplicationUser).FirstOrDefaultAsync(j => j.Id == id);
            if (job == null || job.ApplicationUser == null)
            {
                return NotFound();
            }

            job.Status = ApprovalStatus.ApprovedEdit;
            job.IsActive = true;
            await _context.SaveChangesAsync();

            // Create notification for the employer
            var notification = new Notification
            {
                UserId = job.ApplicationUser.Id,
                Message = $"Công việc '{job.JobTitle}' đã được duyệt chỉnh sửa.",
                CreatedAt = DateTime.Now,
                IsRead = false,
                Type = "ApproveEdit"
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Send notification via SignalR
            await _notificationHub.Clients.User(job.ApplicationUser.Id).SendAsync("ReceiveNotification", notification.Message);

            return RedirectToAction("EditJobsByUser", new { userId = job.ApplicationUserId });
        }

        [HttpPost]
        public async Task<IActionResult> RejectEditedJob(int id, string reason)
        {
            var job = await _context.Jobs.Include(j => j.ApplicationUser).FirstOrDefaultAsync(j => j.Id == id);
            if (job == null || job.ApplicationUser == null)
            {
                return NotFound();
            }

            job.Status = ApprovalStatus.RejectedEdit;
            job.IsActive = false;
            job.RejectionReason = reason; // Store the rejection reason in the database
            await _context.SaveChangesAsync();

            // Create notification for the employer
            var notification = new Notification
            {
                UserId = job.ApplicationUser.Id,
                Message = $"Công việc '{job.JobTitle}' đã bị từ chối chỉnh sửa lý do: {reason}",
                CreatedAt = DateTime.Now,
                IsRead = false,
                Type = "RejectEdit"
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Send notification via SignalR
            await _notificationHub.Clients.User(job.ApplicationUser.Id).SendAsync("ReceiveNotification", notification.Message);

            return RedirectToAction("EditJobsByUser", new { userId = job.ApplicationUserId });
        }





        public async Task<IActionResult> EditJobDetails(int id)
        {
            var editedJob = await _context.Jobs
                .Include(j => j.ApplicationUser)
                .Include(j => j.District)
                .Include(j => j.Ward)
                .Include(j => j.JobCategory)
                .FirstOrDefaultAsync(j => j.Id == id);

            if (editedJob == null)
            {
                return NotFound();
            }

            ViewBag.EditRequestDetailsMessage = editedJob.JobEditRequestDetails ?? "No edit request details were provided for this job.";

            return View(editedJob);
        }








    }
}
